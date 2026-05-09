namespace Client.Models;

public class Customer
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public decimal Balance { get; set; }
}
