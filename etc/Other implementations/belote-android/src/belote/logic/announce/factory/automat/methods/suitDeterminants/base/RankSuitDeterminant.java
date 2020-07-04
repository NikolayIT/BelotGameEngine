package belote.logic.announce.factory.automat.methods.suitDeterminants.base;

import java.util.ArrayList;
import java.util.Iterator;

import belote.bean.Player;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitIterator;

public abstract class RankSuitDeterminant implements SuitDeterminant {

    private final ArrayList<Rank> ranks = new ArrayList<Rank>();

    @Override
    public final Suit determineSuit(Player player) {
        for (SuitIterator iterator = Suit.iterator(); iterator.hasNext();) {
            final Suit suit = iterator.next();

            if (containRanks(player, suit)) {
                return suit;
            }
        }
        return null;
    }

    public final void addRank(final Rank rank) {
        ranks.add(rank);
    }

    private boolean containRanks(Player player, Suit suit) {
        for (Iterator<Rank> iterator = ranks.iterator(); iterator.hasNext();) {
            final Rank rank = iterator.next();
            if (player.getCards().findCard(rank, suit) == null) {
                return false;
            }
        }
        return true;
    }
}
