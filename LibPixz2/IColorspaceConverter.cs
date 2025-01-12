using Game;

namespace Mlfk
{
    public interface IColorspaceConverter
    {
        Color2 ConvertToRgb(Info info);

        Info ConvertFromRgb(Color2 rgb);
    }
}