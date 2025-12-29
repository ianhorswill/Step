namespace Step.Terms
{
    public static class Extensions
    {
        extension(object? x)
        {
            public string ToStringNullTolerant() => x?.ToString() ?? "null";
            public bool EqualsNullTolerant(object? y) => x?.Equals(y) ?? (y == x);
        }
    }
}
