namespace Frotz.Screen
{

    public readonly struct CharDisplayInfo
    {
        public readonly int Font;
        public readonly int Style;
        public readonly int BackgroundColor;
        public readonly int ForegroundColor;

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