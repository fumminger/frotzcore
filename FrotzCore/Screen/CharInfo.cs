namespace Frotz.Screen
{

    public struct CharInfo
    {
        public char Character;
        public CharDisplayInfo DisplayInfo;
        public CharInfo(char character, CharDisplayInfo displayInfo)
        {
            Character = character;
            DisplayInfo = displayInfo;
        }
    }
}