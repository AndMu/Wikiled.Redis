namespace Wikiled.Redis.Scripts
{
    public interface IScriptGenerator
    {
        string GenerateInsertScript(bool trim, int addRecords);
    }
}