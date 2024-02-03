using System.IdentityModel.Tokens.Jwt;

namespace jwtTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        string token = "CfDJ8F17HQZJZQpGrDlElWNxmeLOIpzd3MFXmCx-iQ-1Y_vJlmaxFhceTfWBaUiFpUatrzAm87kChe8MTl8kmPtWKNbCJ2unvTrvvTytWg6FAbLrgoS3A0Iz4FCagKWJIOj9NPSk112Mfo8YE7rg2awT_-jMT329hSEjKyivJ9AI8XbFe5tt09D-vFYH3vNkScwzzV2WoCo4Qm4bPGPrdDgitYp4djD2Mch8fA2bVYMGFWUgCXGxOzQku9_AYP7H1pPksIp7wbqHatbApcWs-mPV_ZdT2sG9dlG6flyEwDHlQNJ3K6Zb1TOXXQ84RjctBwggCDqttiLns8XEIminMbiPrlfiRm5IMnMsmyVrMdkQxww0les3h1LPE2KSpOdTj2c0AMk5YmqK4Gs3w6x_pOZvVQUC6iNrIyA_sdgLdSpe9bMLob5VQ4BCM5Vmv8epkCOV_TkzAC1H-Y1FCKEf3c6IHXVE9VjkJFklaaIjUhDFD99LDn3m41zgtek06b_DFDq2K0lv29Xc6hMyNjQFANZeWh-c7ISHJo86xH8ug7mmKlWPARG9PQBp6poeedt1neoBTBpqnRNdONJNLVY6PI7bP5gpKHFWo7O7tumO8PhbzkhRDqTZqQBV3vzgTkrw46OdA8Ga8S14Iu6MzoHnita0VRa917zaq7Ez8OjKGHgzJEyECVq8nwNsmlXZ5YODSR30mw";

        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        JwtSecurityToken decodedToken = handler.ReadJwtToken(token);

        var nameClaim = decodedToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

        Console.WriteLine($"Email: {nameClaim}");
    }
}
