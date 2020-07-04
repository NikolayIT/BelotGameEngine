package belote.logic;

import belote.bean.pack.Pack;
import belote.bean.pack.PackIterator;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;

public final class BlackRedPackOrderTransformer {

    private final Pack pack;

    public BlackRedPackOrderTransformer(Pack pack) {
        this.pack = pack;
    }

    public Pack transform() {
        Pack result = new Pack();

        int blackSuits = (pack.hasSuitCard(Suit.Spade) ? 1 : 0) + (pack.hasSuitCard(Suit.Club) ? 1 : 0);
        int redSuits = (pack.hasSuitCard(Suit.Heart) ? 1 : 0) + (pack.hasSuitCard(Suit.Diamond) ? 1 : 0);

        Suit[] order;

        if (blackSuits > redSuits) {
            order = new Suit[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };
        } else if (blackSuits < redSuits) {
            order = new Suit[] { Suit.Heart, Suit.Spade, Suit.Club, Suit.Diamond };
        } else {
            order = new Suit[] { Suit.Spade, Suit.Heart, Suit.Club, Suit.Diamond };
        }

        for (Suit suit : order) {
            for (final PackIterator packIterator = pack.iterator(); packIterator.hasNext();) {
                Card card = packIterator.next();
                if (card.getSuit().equals(suit)) {
                    result.add(card);
                }
            }
        }

        return result;
    }
}
