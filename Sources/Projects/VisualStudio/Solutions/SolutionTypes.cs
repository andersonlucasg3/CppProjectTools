namespace ProjectTools.Projects.VisualStudio.Solutions;

public struct SolutionGuid
{
    private Guid _guid;

    public static SolutionGuid NewGuid() => new()
    {
        _guid = Guid.NewGuid()
    };

    public static SolutionGuid Parse(string InGuid) => new()
    {
        _guid = Guid.Parse(InGuid)
    };

    public override readonly string ToString() => _guid.ToString("B").ToUpper();
}

public struct SolutionBool
{
    private bool _bValue;

    public override readonly string ToString()
    {
        return _bValue ? "TRUE" : "FALSE";
    }

    public static implicit operator SolutionBool(bool bValue)
    {
        return new()
        {
            _bValue = bValue
        };
    }

    public static implicit operator bool(SolutionBool InSolutionBool)
    {
        return InSolutionBool._bValue;
    }
}
