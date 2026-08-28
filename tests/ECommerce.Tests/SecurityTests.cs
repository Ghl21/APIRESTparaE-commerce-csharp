using ECommerce.Application.Common;
using ECommerce.Infrastructure.Security;
using Xunit;

namespace ECommerce.Tests;

/// <summary>Pruebas del hashing de contraseñas y de los helpers de dominio.</summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_NuncaDevuelveLaContrasenaEnTextoPlano()
    {
        var hash = _hasher.Hash("Segura123$");

        Assert.DoesNotContain("Segura123$", hash);
        Assert.StartsWith("PBKDF2.", hash);
    }

    [Fact]
    public void Hash_GeneraSalDistintaParaLaMismaContrasena()
    {
        var primero = _hasher.Hash("Segura123$");
        var segundo = _hasher.Hash("Segura123$");

        Assert.NotEqual(primero, segundo);
    }

    [Fact]
    public void Verify_DevuelveTrueConLaContrasenaCorrecta()
    {
        var hash = _hasher.Hash("Segura123$");

        Assert.True(_hasher.Verify("Segura123$", hash));
    }

    [Theory]
    [InlineData("Incorrecta1$")]
    [InlineData("segura123$")]
    [InlineData("")]
    public void Verify_DevuelveFalseConCredencialesInvalidas(string password)
    {
        var hash = _hasher.Hash("Segura123$");

        Assert.False(_hasher.Verify(password, hash));
    }

    [Fact]
    public void Verify_DevuelveFalseSiElHashEstaMalFormado()
    {
        Assert.False(_hasher.Verify("Segura123$", "hash-invalido"));
    }
}

/// <summary>Pruebas del generador de slugs.</summary>
public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Café Colombiano 500g", "cafe-colombiano-500g")]
    [InlineData("Electrónica y Cómputo", "electronica-y-computo")]
    [InlineData("  Espacios   Múltiples  ", "espacios-multiples")]
    [InlineData("Símbolos: #$% raros!", "simbolos-raros")]
    public void Generate_NormalizaAcentosYSeparadores(string input, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Generate(input));
    }

    [Fact]
    public void Generate_DevuelveVacioSiLaEntradaEsVacia()
    {
        Assert.Equal(string.Empty, SlugGenerator.Generate("   "));
    }
}

/// <summary>Pruebas de las reglas de precios.</summary>
public class PricingRulesTests
{
    [Fact]
    public void CalculateTax_AplicaElDieciseisPorCiento()
    {
        Assert.Equal(160.00m, PricingRules.CalculateTax(1000m));
    }

    [Fact]
    public void CalculateShipping_EsGratisAlSuperarElUmbral()
    {
        Assert.Equal(0m, PricingRules.CalculateShipping(PricingRules.FreeShippingThreshold));
        Assert.Equal(0m, PricingRules.CalculateShipping(2000m));
    }

    [Fact]
    public void CalculateShipping_CobraTarifaFijaPorDebajoDelUmbral()
    {
        Assert.Equal(PricingRules.StandardShippingCost, PricingRules.CalculateShipping(500m));
    }

    [Fact]
    public void CalculateShipping_NoCobraEnvioSiElCarritoEstaVacio()
    {
        Assert.Equal(0m, PricingRules.CalculateShipping(0m));
    }
}
