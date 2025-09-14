using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManager.Context;
using TaskManager.Model;

namespace TaskManager.Service
{
    public class TaskConfig
    {
        private readonly IMongoCollection<User> _userCollection;

        public TaskConfig(MongoDBContext context)
        {
            _userCollection = context.User;
        }

        private string GetUserId(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new Exception("Token is missing");
            var jwt = token.Replace("Bearer ", "").Trim();

            var tokenHandler = new JwtSecurityTokenHandler();

       

            var securityToken = tokenHandler.ReadJwtToken(jwt);

            var userId = securityToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
            return userId;
        }

        public async Task CreateTask(List<TaskProperty> tasks, string token)
        {
            var userId = GetUserId(token);
            var user = await _userCollection.Find(u => u.id == userId).FirstOrDefaultAsync();
            if (user == null) throw new Exception("User not found");

            var data = user.Task ?? new List<TaskProperty>();
            data.AddRange(tasks);

            await _userCollection.UpdateOneAsync(
                u => u.id == userId,
                Builders<User>.Update.Set(u => u.Task, data)
            );
        }
        public async Task CreateTaskForm(TaskProperty tasks, string token)
        {
            var userId = GetUserId(token);
            var user = await _userCollection.Find(u => u.id == userId).FirstOrDefaultAsync();
            if (user == null) throw new Exception("User not found");

            var data = user.Task ?? new List<TaskProperty>();
            data.AddRange(tasks);

            await _userCollection.UpdateOneAsync(
                u => u.id == userId,
                Builders<User>.Update.Set(u => u.Task, data)
            );
        }

        public async Task<List<TaskProperty>> GetAllTasks(string token)
        {
            var userId = GetUserId(token);
            var user = await _userCollection.Find(u => u.id == userId).FirstOrDefaultAsync();
            return user?.Task ?? new List<TaskProperty>();
        }

        public async Task<List<TaskProperty>> GetTasksByTitle(string token, string keyword)
        {
            var tasks = await GetAllTasks(token);
            return tasks.Where(t => t.title.ToLower().Contains(keyword.ToLower())).ToList();
        }

        public async Task Remove(string token, string taskId)
        {
            var userId = GetUserId(token);
            var user = await _userCollection.Find(u => u.id == userId).FirstOrDefaultAsync();
            if (user == null) return;

            var tasks = user.Task ?? new List<TaskProperty>();
            tasks = tasks.Where(t => t.Id != taskId).ToList();

            await _userCollection.UpdateOneAsync(
                u => u.id == userId,
                Builders<User>.Update.Set(u => u.Task, tasks)
            );
        }
        public async Task<bool> UpdatePartialTaskAsync(string userId, string taskId, BsonDocument updates)
        {
            var userID = GetUserId(userId);

            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.id, userID),
                Builders<User>.Filter.ElemMatch(u => u.Task, t => t.Id == taskId)
            );

            var update = Builders<User>.Update.Combine(
                updates.Elements.Select(kv =>
                    Builders<User>.Update.Set($"Task.$.{kv.Name}", kv.Value))
            );

            var result = await _userCollection.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }



        public async Task ToggleSubTask(string token, string taskId, string subTaskId)
        {
            var userId = GetUserId(token);
            var user = await _userCollection.Find(u => u.id == userId).FirstOrDefaultAsync();
            if (user == null) return;

            var tasks = user.Task ?? new List<TaskProperty>();
            var task = tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null && task.subTasks != null)
            {
                var subTask = task.subTasks.FirstOrDefault(st => st.Id == subTaskId);
                if (subTask != null)
                {
                    subTask.done = !subTask.done;
                    await _userCollection.UpdateOneAsync(
                        u => u.id == userId,
                        Builders<User>.Update.Set(u => u.Task, tasks)
                    );
                }
            }
        }

        public async Task<List<TaskProperty>> Filter(string token, TaskFilter filter)
        {
            var tasks = await GetAllTasks(token);

            if (!string.IsNullOrEmpty(filter.Priority))
                tasks = tasks.Where(t => t.priority.Equals(filter.Priority, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(filter.Status))
                tasks = tasks.Where(t => t.status.Equals(filter.Status, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(filter.DateOrder))
                tasks = filter.DateOrder == "newest"
                    ? tasks.OrderByDescending(t => t.createdAt).ToList()
                    : tasks.OrderBy(t => t.createdAt).ToList();

            if (!string.IsNullOrEmpty(filter.PriorityOrder))
                tasks = filter.PriorityOrder == "asc"
                    ? tasks.OrderBy(t => t.priority).ToList()
                    : tasks.OrderByDescending(t => t.priority).ToList();

            return tasks;
        }

        // 📊 taskByMonth
        public async Task<object> TaskByMonth(string token)
        {
            var tasks = await GetAllTasks(token);
            var grouped = tasks
                .GroupBy(t => t.startedAt.HasValue ? t.startedAt.Value.ToString("MM-yyyy") : "Unknown")
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Completed = g.Count(t => t.status == "completed"),
                        Pending = g.Count(t => t.status == "pending"),
                        InProgress = g.Count(t => t.status == "in-progress")
                    }
                );
            return grouped;
        }

        // 📊 insights
        public async Task<object> Insights(string token)
        {
            var tasks = await GetAllTasks(token);
            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.status == "completed");

            double avgProcrastinationScore = 0;
            if (tasks.Count > 0)
            {
                var scores = new List<double>();
                foreach (var task in tasks)
                {
                    scores.Add(await CalculateProcrastinationScore(task));
                }
                avgProcrastinationScore = scores.Average();
            }

            double estimationAccuracy = 0;
            var completedWithEstimates = tasks.Where(t => t.status == "completed" && t.estimateMin.HasValue && t.actualMin.HasValue).ToList();
            if (completedWithEstimates.Count > 0)
            {
                var accuracyList = completedWithEstimates
                    .Select(t => Math.Abs(t.estimateMin.Value - t.actualMin.Value) / (double)t.estimateMin.Value)
                    .ToList();

                estimationAccuracy = 1 - accuracyList.Average();
                estimationAccuracy = Math.Round(estimationAccuracy * 100, 2);
            }

            return new
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                AverageProcrastinationScore = Math.Round(avgProcrastinationScore, 2),
                EstimationAccuracy = estimationAccuracy
            };
        }

       
        public async Task<double> CalculateProcrastinationScore(TaskProperty task)
        {
            var now = DateTime.UtcNow;
            var createdTime = task.createdAt;
            double daysSinceCreated = (now - createdTime).TotalDays;
            double score = 0;
            if (daysSinceCreated > 1) score += 0.2;
            if (daysSinceCreated > 3) score += 0.3;
            if (daysSinceCreated > 7) score += 0.4;

            var priorityWeight = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { TaskConstants.priority.Urgent, 0.4 },
                { TaskConstants.priority.High, 0.3 },
                { TaskConstants.priority.Medium, 0.2 },
                { TaskConstants.priority.Low, 0.1 }
            };

            double weight = priorityWeight.ContainsKey(task.priority) ? priorityWeight[task.priority] : 0.0;
            score += weight;

            if (task.dueAt.HasValue)
            {
                var dueTime = task.dueAt.Value;
                double daysUntilDue = (dueTime - now).TotalDays;
                if (daysUntilDue < 0) score += 0.5;
                else if (daysUntilDue < 1) score += 0.3;
                else if (daysUntilDue < 3) score += 0.2;
            }

            if (task.status == "completed" && (task.progress ?? 0) < 20)
                score = 0.2;

            return score;
        }

        public Task<string> GetAIAdvice(TaskProperty task, double score)
        {
            var dueTime = task.dueAt.HasValue ? Math.Ceiling((task.dueAt.Value - DateTime.UtcNow).TotalDays) : double.MaxValue;

            if (dueTime < 0)
                return Task.FromResult("⚠️ This task is already overdue! Prioritize finishing it immediately before starting new tasks.");

            if (dueTime <= 2)
            {
                if (score < 0.5) return Task.FromResult("⏰ The deadline is very close. Stay focused and complete the most important parts first.");
                else return Task.FromResult("🚨 The deadline is critical and progress is behind. Eliminate all distractions and work on this task as your top priority.");
            }

            if (dueTime <= 7)
            {
                if (score < 0.3) return Task.FromResult("✅ You're doing well and have about a week left. Keep your pace steady.");
                else if (score < 0.6) return Task.FromResult("📌 Break the task into smaller goals to make steady progress before the deadline.");
                else return Task.FromResult("⚡ You're falling behind. Plan your schedule carefully this week to catch up.");
            }

            if (score < 0.3) return Task.FromResult("👍 Plenty of time and good progress so far. Just keep your consistency.");
            else if (score < 0.6) return Task.FromResult("📝 You still have time. Organize subtasks now so you won't rush later.");
            else return Task.FromResult("⚠️ Even with a distant deadline, your progress is lagging. Start focusing earlier to avoid last-minute stress.");

            return Task.FromResult("Keep working on your task step by step.");
        }
    }
}
