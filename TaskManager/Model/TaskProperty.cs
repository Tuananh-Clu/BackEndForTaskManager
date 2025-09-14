using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class TaskProperty
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; }

    [Required]
    public string title { get; set; }

    public string? description { get; set; }

    [Required]
    public string status { get; set; } 

    [Required]
    public string priority { get; set; }

    public List<string>? tags { get; set; }

    public int? estimateMin { get; set; }

    public int? actualMin { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? startedAt { get; set; }

    public DateTime? dueAt { get; set; }

    public string completedAt { get; set; }

    public string repeat { get; set; } = "none";

    public DateTime? reminderAt { get; set; }

    public double? progress { get; set; }

    public List<subTask>? subTasks { get; set; }

    public double? procrastinationFactor { get; set; }

    public string? color { get; set; }
}

public class subTask
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; }

    public string title { get; set; }

    public bool done { get; set; }
}

public static class TaskConstants
{
    public static class status
    {
        public const string Pending = "pending";
        public const string InProgress = "in-progress";
        public const string Completed = "completed";

        public static readonly string[] AllValues = { Pending, InProgress, Completed };
    }

    public static class priority
    {
        public const string Low = "low";
        public const string Medium = "medium";
        public const string High = "high";
        public const string Urgent = "urgent";

        public static readonly string[] AllValues = { Low, Medium, High, Urgent };
    }

    public static class repeatType
    {
        public const string None = "none";
        public const string Daily = "daily";
        public const string Weekly = "weekly";
        public const string Monthly = "monthly";

        public static readonly string[] AllValues = { None, Daily, Weekly, Monthly };
    }
}