namespace AppointmentAPP.Models.Entity
{
    public abstract class BaseEntity : AuditEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public abstract class AuditEntity 
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
