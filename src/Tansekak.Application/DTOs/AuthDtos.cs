namespace Tansekak.Application.DTOs;

public record LoginRequestDto(string Email, string Password);
public record AuthUserDto(string Email, string Role);
