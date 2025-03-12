namespace Common.Models
{
    public class NewcomerData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FullName { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string Bio { get; set; }
        public string PhotoUrl { get; set; }
        public string[] Hobbies { get; set; }
        public DateTime JoiningDate { get; set; }
    }
}