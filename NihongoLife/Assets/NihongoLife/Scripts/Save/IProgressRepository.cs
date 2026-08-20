using NihongoLife.Core;
using NihongoLife.Data;

namespace NihongoLife.Save
{
    public interface IProgressRepository : IGameService
    {
        PlayerProgressDto GetProgress();
        void SaveProgress(PlayerProgressDto progress);
        void ResetProgress();
    }
}
