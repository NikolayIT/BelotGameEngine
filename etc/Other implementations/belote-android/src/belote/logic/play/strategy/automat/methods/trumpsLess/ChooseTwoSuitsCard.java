/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumpsLess;

import belote.base.IntegerIterator;
import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitCountHashTable;
import belote.bean.pack.card.suit.SuitIterator;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * ChooseTwoSuitsCard class. PlayCardMethod which implements the logic of playing a card in the situation when the player has 4 cards and 2 of them are
 * from one suits the other 2 from another.
 * @author Dimitar Karamanov
 */
public final class ChooseTwoSuitsCard extends BaseMethod {

    /**
     * Constant used in logic
     */
    public static final int TWO = 2;

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public ChooseTwoSuitsCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        Card result = null;
        final SuitCountHashTable suits = player.getCards().getSuitsDistribution();

        if (isTwoSuitsWithTwoCasrdsDistribution(suits)) {
            final Player partner = player.getPartner();
            for (final SuitIterator iterator = suits.suitIterator(); iterator.hasNext();) {
                final Suit suit = iterator.next();
                if (result == null) {
                    result = player.getCards().findMinSuitCard(suit);
                } else {
                    if (partner.getUnwantedSuits().contains(suit)) {
                        final int suitIndex = partner.getUnwantedSuits().getSuitIndex(suit);
                        final int resultIndex = partner.getUnwantedSuits().getSuitIndex(result.getSuit());

                        if (suitIndex > resultIndex && resultIndex != -1) {
                            result = player.getCards().findMinSuitCard(suit);
                        }
                    } else {
                        result = player.getCards().findMinSuitCard(suit);
                    }
                }
            }
        }
        return result;
    }

    /**
     * Returns if the provided hashtable contains 2 suits with 2 counts.
     * @param suits SuitCountHashTable which contains Suit as key and Integer value.
     * @return true if suits contains 2 suit with counts == 2, false otherwise.
     */
    private boolean isTwoSuitsWithTwoCasrdsDistribution(final SuitCountHashTable suits) {
        if (suits.size() == TWO) {
            for (final IntegerIterator iterator = suits.countIterator(); iterator.hasNext();) {
                if (iterator.next().intValue() != TWO) {
                    return false;
                }
            }
            return true;
        }
        return false;
    }
}