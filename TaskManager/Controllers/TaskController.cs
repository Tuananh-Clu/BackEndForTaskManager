using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManager.Model;
using TaskManager.Service;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly TaskConfig _task;

        public TaskController(TaskConfig config)
        {
            _task = config;
        }

        [HttpGet("GetAllTask")]
        public async Task<IActionResult> GetAllTask([FromHeader(Name = "Authorization")] string token)
        {
            var response = await _task.GetAllTasks(token);
            return Ok(response);
        }


        [HttpPost("AddOneTask")]
        public async Task<IActionResult> CreateTaskForm([FromHeader(Name = "Authorization")] string token, [FromBody] TaskProperty tasks)
        {
            await _task.CreateTaskForm(tasks, token);
            return Ok("Task created successfully");
        }

        [HttpPost("AddTask")]
        public async Task<IActionResult> CreateTask([FromHeader(Name = "Authorization")] string token, [FromBody] List<TaskProperty> tasks)
        {
            await _task.CreateTask(tasks, token);
            return Ok("Task created successfully");
        }

        [HttpGet("GetTaskByTitle")]
        public async Task<IActionResult> GetTaskByTitle([FromHeader(Name = "Authorization")] string token, [FromQuery] string keyword)
        {
            var data = await _task.GetTasksByTitle(token, keyword);
            return Ok(data);
        }

        [HttpDelete("RemoveTask")]
        public async Task<IActionResult> Remove([FromHeader(Name = "Authorization")] string token, [FromQuery] string id)
        {
            await _task.Remove(token, id);
            return Ok("Task removed successfully");
        }

        [HttpPatch("UpdateTask")]
        public async Task<IActionResult> PatchTask(
      [FromHeader(Name = "Authorization")] string token,
      [FromQuery] string id,
      [FromBody] JsonDocument updates)
        {
            if (updates == null || updates.RootElement.ValueKind != JsonValueKind.Object)
                return BadRequest("Invalid update object.");

            var bsonUpdates = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(updates.RootElement.GetRawText());

            var success = await _task.UpdatePartialTaskAsync(token, id, bsonUpdates);

            if (!success)
                return NotFound(new { message = "Task not found or nothing updated." });

            return Ok(new { message = "Task updated successfully." });
        }


        [HttpPut("ToggleSubTask")]
        public async Task<IActionResult> ToggleSubTask([FromHeader(Name = "Authorization")] string token, [FromQuery] string id, [FromQuery] string subTaskId)
        {
            await _task.ToggleSubTask(token, id, subTaskId);
            return Ok("SubTask toggled successfully");
        }

        [HttpPost("FilterTask")]
        public async Task<IActionResult> Filter([FromHeader(Name = "Authorization")] string token, [FromBody] TaskFilter filter)
        {
            var response = await _task.Filter(token, filter);
            return Ok(response);
        }

        [HttpPost("CalculateProcrastinationFactor")]
        public async Task<IActionResult> CalculateProcrastinationFactor([FromBody] TaskProperty taskProperty)
        {
            if (taskProperty.estimateMin == null || taskProperty.actualMin == null || taskProperty.estimateMin == 0)
            {
                return BadRequest("EstimateMin and ActualMin must be provided and EstimateMin must be greater than zero.");
            }

            double score = await _task.CalculateProcrastinationScore(taskProperty);
            return Ok(score);
        }

        [HttpPost("GetAISuggestion")]
        public async Task<IActionResult> GetAISuggestion([FromBody] TaskProperty taskProperty, [FromQuery] double score)
        {
            var response = await _task.GetAIAdvice(taskProperty, score);
            return Ok(response);
        }

        [HttpGet("GetTaskByMonth")]
        public async Task<IActionResult> GetTaskByMonth([FromHeader(Name = "Authorization")] string token)
        {
            var response = await _task.TaskByMonth(token);
            return Ok(response);
        }

        [HttpGet("GetInsights")]
        public async Task<IActionResult> GetInsights([FromHeader(Name = "Authorization")] string token)
        {
            var response = await _task.Insights(token);
            return Ok(response);
        }
    }
}
