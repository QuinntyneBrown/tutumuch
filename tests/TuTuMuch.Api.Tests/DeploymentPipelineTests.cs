// Architecture Test
// Traces to: L2-084, L2-085, L2-086
// Description: The deploy workflow, compose stack and Caddyfile deploy only green pushes to main, migrate before
// the restart, fail on a bad health check, keep secrets scoped, fit a small VM and never cache index.html.

using System.Text.RegularExpressions;

namespace TuTuMuch.Api.Tests;

public sealed partial class DeploymentPipelineTests
{
    private static readonly string Root = FindRepositoryRoot();
    private static readonly string Workflow = Read(".github/workflows/deploy.yml");
    private static readonly string Compose = Read("deploy/docker-compose.yml");
    private static readonly string Caddyfile = Read("deploy/web/Caddyfile");

    [Fact]
    public void Deploy_runs_after_a_successful_ci_push_to_main()
    {
        Assert.Matches(@"workflow_run:\s*\n\s+workflows: \[CI\]\s*\n\s+types: \[completed\]\s*\n\s+branches: \[main\]", Workflow);
        Assert.Contains("github.event.workflow_run.conclusion == 'success'", Workflow);
        Assert.Contains("github.event.workflow_run.event == 'push'", Workflow);
        Assert.DoesNotMatch(@"\n  push:", Workflow);
    }

    [Fact]
    public void Deploy_can_be_run_by_hand_for_any_sha_as_a_rollback()
    {
        Assert.Contains("workflow_dispatch:", Workflow);
        Assert.Matches(@"inputs:\s*\n\s+ref:", Workflow);
        Assert.Contains("inputs.ref || github.event.workflow_run.head_sha || github.sha", Workflow);
        Assert.Contains("ref: ${{ env.DEPLOY_SHA }}", Workflow);
        Assert.Contains("IMAGE_TAG=${{ env.DEPLOY_SHA }}", Workflow);
    }

    [Fact]
    public void Deploys_never_overlap()
    {
        Assert.Matches(@"concurrency:\s*\n\s+group: deploy-production\s*\n\s+cancel-in-progress: false", Workflow);
    }

    [Fact]
    public void Migrations_run_before_the_restart_and_a_failure_stops_it()
    {
        var script = Workflow[Workflow.IndexOf("bash -s <<'REMOTE'", StringComparison.Ordinal)..];
        var strict = script.IndexOf("set -euo pipefail", StringComparison.Ordinal);
        var migrate = script.IndexOf("docker compose run --rm -T migrator < /dev/null", StringComparison.Ordinal);
        var restart = script.IndexOf("docker compose up -d", StringComparison.Ordinal);

        Assert.True(strict >= 0 && migrate > strict && restart > migrate, "Expected set -euo pipefail, then the migrator, then up -d.");
    }

    [Fact]
    public void A_failed_health_check_fails_the_run()
    {
        Assert.Contains("curl -fsS --max-time 10 \"https://${SITE_HOST}/api/health\" && exit 0", Workflow);
        Assert.Contains("echo \"health check failed\" && exit 1", Workflow);
    }

    [Fact]
    public void Only_the_build_job_may_write_packages_and_deploy_uses_the_production_environment()
    {
        Assert.Single(PackagesWrite().Matches(Workflow));
        var build = Job("build");
        var deploy = Job("deploy");

        Assert.Contains("packages: write", build);
        Assert.Contains("environment: production", deploy);
        Assert.DoesNotContain("packages: write", deploy);
        Assert.Matches(@"^permissions: \{\}", Workflow.Split('\n').First(line => line.StartsWith("permissions:", StringComparison.Ordinal)));
    }

    [Fact]
    public void Secrets_are_never_echoed_and_the_key_is_private()
    {
        Assert.DoesNotMatch(@"echo[^\n]*secrets\.", Workflow);
        Assert.Contains("umask 077", Workflow);
        Assert.Contains("--password-stdin", Workflow);
    }

    [Theory]
    [InlineData("web", "128m")]
    [InlineData("api", "512m")]
    [InlineData("worker", "384m")]
    [InlineData("migrator", "384m")]
    public void Each_service_fits_a_small_vm_with_rotated_logs(string service, string memory)
    {
        var block = Service(service);

        Assert.Contains($"mem_limit: {memory}", block);
        Assert.Contains("logging: *logging", block);
        Assert.DoesNotContain("replicas", block);
    }

    [Fact]
    public void Logs_rotate_through_json_file()
    {
        Assert.Matches(@"x-logging: &logging\s*\n\s+driver: json-file\s*\n\s+options:\s*\n\s+max-size: ""10m""\s*\n\s+max-file: ""3""", Compose);
    }

    [Fact]
    public void Index_html_and_spa_routes_are_not_cached_but_hashed_assets_are()
    {
        Assert.Contains("@entry not path *.js *.css", Caddyfile);
        Assert.Contains("header @entry Cache-Control \"no-cache\"", Caddyfile);
    }

    private static string Job(string name)
    {
        var start = Workflow.IndexOf($"\n  {name}:\n", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Job {name} not found.");
        var next = TopLevelKey().Match(Workflow, start + 1);
        return Workflow[start..(next.Success ? next.Index : Workflow.Length)];
    }

    private static string Service(string name)
    {
        var start = Compose.IndexOf($"\n  {name}:\n", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Service {name} not found.");
        var next = TopLevelKey().Match(Compose, start + 1);
        return Compose[start..(next.Success ? next.Index : Compose.Length)];
    }

    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path)).ReplaceLineEndings("\n");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root (AGENTS.md) not found.");
    }

    [GeneratedRegex(@"\n  [a-z-]+:\n|\n[a-z]")]
    private static partial Regex TopLevelKey();

    [GeneratedRegex("packages: write")]
    private static partial Regex PackagesWrite();
}
