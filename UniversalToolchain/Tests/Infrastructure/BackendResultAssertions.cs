namespace Tests.Infrastructure;

internal static class BackendResultAssertions
{
    public static void AssertEquivalent(object? left, object? right)
    {
        var normalizedLeft = BackendValueNormalizer.Normalize(left);
        var normalizedRight = BackendValueNormalizer.Normalize(right);

        if (normalizedLeft is null || normalizedRight is null)
        {
            Assert.That(normalizedLeft, Is.EqualTo(normalizedRight));
            return;
        }

        if (normalizedLeft is bool || normalizedRight is bool)
        {
            Assert.That((bool)normalizedLeft, Is.EqualTo((bool)normalizedRight));
            return;
        }

        if (normalizedLeft is double leftDouble && normalizedRight is double rightDouble)
        {
            Assert.That(leftDouble, Is.EqualTo(rightDouble).Within(1e-9));
            return;
        }

        Assert.That(normalizedLeft, Is.EqualTo(normalizedRight));
    }
}
