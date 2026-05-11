using System.Collections.Concurrent;

namespace OfisYonetimSistemi.Services;

public sealed class LoginAttemptTracker
{
    private const int EmailFailedAttemptLimit = 5;
    private const int IpFailedAttemptLimit = 20;
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, AttemptState> _attempts = new();

    public LoginLockoutStatus GetLockoutStatus(string email, string ipAddress)
    {
        var now = DateTimeOffset.UtcNow;
        var emailStatus = GetKeyStatus(EmailKey(email), now);
        var ipStatus = GetKeyStatus(IpKey(ipAddress), now);

        if (emailStatus.IsLocked && ipStatus.IsLocked)
        {
            return emailStatus.RetryAfter >= ipStatus.RetryAfter ? emailStatus : ipStatus;
        }

        return emailStatus.IsLocked ? emailStatus : ipStatus;
    }

    public LoginLockoutStatus RecordFailedAttempt(string email, string ipAddress)
    {
        var now = DateTimeOffset.UtcNow;
        var emailStatus = RecordFailedAttempt(EmailKey(email), EmailFailedAttemptLimit, now);
        var ipStatus = RecordFailedAttempt(IpKey(ipAddress), IpFailedAttemptLimit, now);

        if (emailStatus.IsLocked && ipStatus.IsLocked)
        {
            return emailStatus.RetryAfter >= ipStatus.RetryAfter ? emailStatus : ipStatus;
        }

        return emailStatus.IsLocked ? emailStatus : ipStatus;
    }

    public void ResetEmail(string email)
    {
        _attempts.TryRemove(EmailKey(email), out _);
    }

    private LoginLockoutStatus GetKeyStatus(string key, DateTimeOffset now)
    {
        if (!_attempts.TryGetValue(key, out var state))
        {
            return LoginLockoutStatus.NotLocked;
        }

        lock (state)
        {
            if (state.LockedUntilUtc.HasValue && state.LockedUntilUtc.Value > now)
            {
                return LoginLockoutStatus.Locked(state.LockedUntilUtc.Value - now);
            }

            if (state.WindowStartedUtc + AttemptWindow < now)
            {
                _attempts.TryRemove(key, out _);
            }

            return LoginLockoutStatus.NotLocked;
        }
    }

    private LoginLockoutStatus RecordFailedAttempt(string key, int limit, DateTimeOffset now)
    {
        var state = _attempts.GetOrAdd(key, _ => new AttemptState(now));

        lock (state)
        {
            if (state.LockedUntilUtc.HasValue && state.LockedUntilUtc.Value > now)
            {
                return LoginLockoutStatus.Locked(state.LockedUntilUtc.Value - now);
            }

            if (state.WindowStartedUtc + AttemptWindow < now)
            {
                state.FailedCount = 0;
                state.WindowStartedUtc = now;
                state.LockedUntilUtc = null;
            }

            state.FailedCount++;

            if (state.FailedCount >= limit)
            {
                state.LockedUntilUtc = now + LockoutDuration;
                return LoginLockoutStatus.Locked(LockoutDuration);
            }
        }

        return LoginLockoutStatus.NotLocked;
    }

    private static string EmailKey(string email)
    {
        return $"email:{email.Trim().ToLowerInvariant()}";
    }

    private static string IpKey(string ipAddress)
    {
        return $"ip:{ipAddress}";
    }

    private sealed class AttemptState
    {
        public AttemptState(DateTimeOffset windowStartedUtc)
        {
            WindowStartedUtc = windowStartedUtc;
        }

        public int FailedCount { get; set; }
        public DateTimeOffset WindowStartedUtc { get; set; }
        public DateTimeOffset? LockedUntilUtc { get; set; }
    }
}

public readonly record struct LoginLockoutStatus(bool IsLocked, TimeSpan RetryAfter)
{
    public static LoginLockoutStatus NotLocked => new(false, TimeSpan.Zero);

    public static LoginLockoutStatus Locked(TimeSpan retryAfter)
    {
        return new(true, retryAfter < TimeSpan.Zero ? TimeSpan.Zero : retryAfter);
    }
}
