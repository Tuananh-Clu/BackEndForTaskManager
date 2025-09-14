namespace TaskManager.Model
{
    public class User
    {
        public string id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public List<TaskProperty> Task { get; set; }
    }
}
