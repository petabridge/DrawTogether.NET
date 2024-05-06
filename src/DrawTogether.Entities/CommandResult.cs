namespace DrawTogether.Entities;

public enum ResultCode
{
    Ok,
    NoOp,
    BadRequest,
    Unauthorized,
    TimeOut
}

public sealed record CommandResult
{
    public ResultCode Code { get; init; }
    
    public string? Message { get; init; }
    
    public static readonly CommandResult Ok = new() { Code = ResultCode.Ok };
    
    public static readonly CommandResult NoOp = new() { Code = ResultCode.NoOp };
    
    public bool IsError => Code != ResultCode.Ok && Code != ResultCode.NoOp;
}