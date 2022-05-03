namespace Frotz.Screen
{

    public readonly struct CharInfo
    {
        public readonly char Character;
        public readonly CharDisplayInfo DisplayInfo;
        public CharInfo(char character, CharDisplayInfo displayInfo)
        {
            Character = character;
            DisplayInfo = displayInfo;
        }
    }
}