
namespace dsstats.shared.Auth;

public interface IAuthService
{
    Task<IForgotPasswordResponse> ForgotPassword(string email);
    Task<ILoginResponse> Login(LoginPayload login);
    Task<IManageInfoResponse> ManageInfo(ManageInfoPayload manage);
    Task<ILoginResponse> Refresh(string refreshToken);
    Task<IRegisterResponse> Register(RegisterPayload register);
    Task<bool> ResendConfirmationEmail(string email);
    Task<IForgotPasswordResponse> ResetPassword(ResetPaylaod reset);
}

public interface ILoginResponse { }
public record LoginSucessResponse : ILoginResponse
{
    public TokenInfo TokenInfo { get; set; } = new();
}

public record LoginFailedResponse : ILoginResponse
{
    public string Error { get; set; } = string.Empty;
}

public interface IRegisterResponse { }

public record RegisterSuccessResponse : IRegisterResponse { }
public record RegisterFailedResponse : IRegisterResponse
{
    public ErrorResponse Error { get; set; } = new();
}

public interface IForgotPasswordResponse { }
public record ForgotPasswordSuccessResponse : IForgotPasswordResponse { };
public record ForgotPasswordFailedResponse : IForgotPasswordResponse
{
    public ErrorResponse Error { get; set; } = new();
}

public interface IManageInfoResponse { }
public record ManageInfoSuccessResponse : IManageInfoResponse
{
    public ManageInfoResponse Response { get; set; } = new();
}
public record ManageInfoFailedResponse : IManageInfoResponse
{
    public ErrorResponse Error { get; set; } = new();
}