using System.Text.RegularExpressions;

var commitMsgFile = Args[0];
var commitMsg = File.ReadAllText(commitMsgFile).Trim();

var pattern = @"^(feat|fix|docs|style|refactor|perf|test|chore)\((domain|app|infra|api|ia|db)\): .{1,}$";

if (!Regex.IsMatch(commitMsg, pattern))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: Formato de mensaje de commit inválido.");
    Console.WriteLine("El formato debe ser: <tipo>(<alcance>): <descripción>");
    Console.WriteLine("\nTipos válidos: feat, fix, docs, style, refactor, perf, test, chore");
    Console.WriteLine("Alcances válidos: domain, app, infra, api, ia, db");
    Console.WriteLine("\nEjemplo: feat(domain): agregar entidad de auditoría");
    Console.ResetColor();
    Environment.Exit(1);
}

Environment.Exit(0);
