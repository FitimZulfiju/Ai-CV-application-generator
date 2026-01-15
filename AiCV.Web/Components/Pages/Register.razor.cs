namespace AiCV.Web.Components.Pages;

public partial class Register
{
    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    private string _email = "";
    private string _password = "";
    private string _confirmPassword = "";
}
