namespace BuildTools
{
    public interface IBuildTool
    {

        string Name { get; }

        void Run(string[] args);

    }
}