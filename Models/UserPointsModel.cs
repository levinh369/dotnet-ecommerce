namespace ProjectTest1.Models
{
    public class UserPointsModel
    {
        [ConfigurationKeyName("Id")]
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int TotalPoints { get; set; } = 0;
        public int LifetimePoints { get; set; } = 0;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public UserModel User { get; set; } = null!;
    }
}
