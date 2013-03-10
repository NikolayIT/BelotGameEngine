namespace JustBelot.Common
{
    // TODO: Rename to CardsCombinationType
    public enum CardsCombinationType
    {
        Tierce = 20, // A sequence of three (sequences are in the "A K Q J 10 9 8 7" order of the same suit) — is worth 20 points
        Quart = 50, // A sequence of four — is worth 50 points.
        Quint = 100, // A sequence of five — is worth 100 points (longer sequences are not awarded)

        FourOfAKind = 100, // Four of Aces, Kings, Queens, or Tens is worth 100 points. (Sevens and Eights are not awarded.)
        FourOfNines = 150, // Four of Nines is worth 150 points.
        FourOfJacks = 200, // Four of Jacks is worth 200 points.
    }
}
