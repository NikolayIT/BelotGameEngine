/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack;

import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;

/**
 * TrickPack class.
 * @author Dimitar Karamanov
 */
public final class TrickPack extends Pack {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 7015330407925820737L;

    /**
     * Constructor.
     */
    public TrickPack() {
        super();
    }

    /**
     * Returns attack card.
     * @return Card the attack card.
     */
    public Card getAttackCard() {
        final PackIterator it = iterator();
        if (it.hasNext()) {
            return it.next();
        }
        return null;
    }

    /**
     * Returns the card with best rank and suit equals to attack card's suit.
     * @return Card with the best(max) rank and suit equals to attack card's suit.
     */
    public Card getHandAttackSuitCard() {
        final PackIterator it = iterator();
        if (it.hasNext()) {
            final Suit suit = it.next().getSuit();
            return findMaxSuitCard(suit);
        }
        return null;
    }
    
    /**
     * Returns the card with best rank and suit equals to attack card's suit.
     * @return Card with the best(max) rank and suit equals to attack card's suit.
     */
    public Card getHandAttackSuitCard(Suit suit) {
        if (suit != null) {
            Card card = findMaxSuitCard(suit);
            if (card != null) {
                return card;
            }
        }
        return getHandAttackSuitCard();
    }
}
