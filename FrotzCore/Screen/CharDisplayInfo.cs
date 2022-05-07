namespace Frotz.Screen
{

    public struct CharDisplayInfo
    {
        public int Font;
        public int Style;
        public int BackgroundColor;
        public int ForegroundColor;

        public CharDisplayInfo(int font, int style, int backgroundColor, int foregroundColor)
        {
            Font = font;
            Style = style;
            BackgroundColor = backgroundColor;
            ForegroundColor = foregroundColor;
        }
        public bool ImplementsStyle(int styleBit) => (Style & styleBit) == styleBit;
    }
}