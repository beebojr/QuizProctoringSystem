using System.Reflection;

namespace QPS.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute : Attribute { }

public sealed class AssertionException(string message) : Exception(message);

public static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new AssertionException(message);
    }

    public static void Equal<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException($"{what}: expected <{expected}>, got <{actual}>");
    }

    public static void Contains(string needle, string haystack, string what)
    {
        if (!haystack.Contains(needle, StringComparison.OrdinalIgnoreCase))
            throw new AssertionException($"{what}: \"{haystack}\" does not contain \"{needle}\"");
    }
}

public static class Program
{
    public static int Main()
    {
        var tests = typeof(Program).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.GetCustomAttribute<TestAttribute>() is not null)
            .OrderBy(m => m.DeclaringType!.Name).ThenBy(m => m.Name)
            .ToList();

        var failed = 0;
        foreach (var test in tests)
        {
            var name = $"{test.DeclaringType!.Name}.{test.Name}";
            try
            {
                test.Invoke(null, null);
                Console.WriteLine($"  PASS  {name}");
            }
            catch (TargetInvocationException ex)
            {
                failed++;
                Console.WriteLine($"  FAIL  {name}\n        {ex.InnerException?.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(failed == 0 ? $"All {tests.Count} tests passed." : $"{failed} of {tests.Count} tests failed.");
        return failed == 0 ? 0 : 1;
    }
}
