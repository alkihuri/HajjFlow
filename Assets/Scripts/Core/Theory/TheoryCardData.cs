using UnityEngine;

namespace Core.Theory
{
    [CreateAssetMenu(menuName = "Theory/CardData")]
    public class TheoryCardData : ScriptableObject
    {
        public string Title;
        public string Description;
        public Sprite Image;
    }


     
}