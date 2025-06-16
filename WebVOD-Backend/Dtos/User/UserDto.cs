namespace WebVOD_Backend.Dtos.User;

public class UserDto
{
    public string Id { get; set; }
    public string Login { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public DateOnly SignupDate { get; set; }
}
