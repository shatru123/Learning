using System.Reflection;
using Xunit;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("==================================================================");
Console.WriteLine("        LearningOS Automated Test & Verification Suite           ");
Console.WriteLine("==================================================================");
Console.ResetColor();

var testClasses = new Type[]
{
    typeof(LearningOS.Tests.DataSurvivalTests),
    typeof(LearningOS.Tests.StreakCalculationTests),
    typeof(LearningOS.Tests.RecoveryLogicTests),
    typeof(LearningOS.Tests.BackupRestoreTests),
    typeof(LearningOS.Tests.ControllerIntegrationTests),
    typeof(LearningOS.Tests.CurriculumIntegrityTests)
};

int total = 0;
int passed = 0;
int failed = 0;
var failures = new List<(string Name, Exception Ex)>();

foreach (var testClass in testClasses)
{
    Console.WriteLine($"\n[Executing Test Fixture]: {testClass.Name}");
    var methods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.GetCustomAttribute<FactAttribute>() != null)
        .ToList();

    foreach (var method in methods)
    {
        total++;
        Console.Write($"  -> Running {method.Name} ... ");
        object? instance = null;
        try
        {
            instance = Activator.CreateInstance(testClass);
            var result = method.Invoke(instance, null);
            if (result is Task asyncTask)
            {
                asyncTask.GetAwaiter().GetResult();
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[PASSED]");
            Console.ResetColor();
            passed++;
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException ?? ex;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAILED]: {inner.Message}");
            Console.ResetColor();
            failed++;
            failures.Add(($"{testClass.Name}.{method.Name}", inner));
        }
        finally
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

Console.WriteLine("\n==================================================================");
if (failed == 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  ALL {passed}/{total} TESTS PASSED SUCCESSFULLY! ZERO FAILURES.");
    Console.ResetColor();
    Console.WriteLine("==================================================================");
    return 0;
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  {failed}/{total} TESTS FAILED.");
    foreach (var (name, ex) in failures)
    {
        Console.WriteLine($"  - {name}: {ex.Message}");
    }
    Console.ResetColor();
    Console.WriteLine("==================================================================");
    return 1;
}
