/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.conditions;

import belote.bean.Player;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceCondition;
import belote.logic.announce.factory.automat.methods.suitDeterminants.base.SuitDeterminant;

/**
 * PlayerCard class. Returns true if the announce player has card from provided rank and suit.
 * @author Dimitar Karamanov
 */
public final class HasCard implements AnnounceCondition {

    /**
     * Cards rank.
     */
    private final Rank rank;

    /**
     * Suit determinant which dynamically by provided player determines the suit.
     */
    private final SuitDeterminant suitDeterminant;

    /**
     * Constructor.
     * @param rank of the card.
     * @param suitDeterminant used to determine the suit.
     */
    public HasCard(final Rank rank, final SuitDeterminant suitDeterminant) {
        this.rank = rank;
        this.suitDeterminant = suitDeterminant;
    }

    /**
     * The method which returns the result of condition.
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    public boolean process(final Player player) {
        final Suit suit = suitDeterminant.determineSuit(player);

        if (suit != null) {
            return player.getCards().findCard(rank, suit) != null;
        }

        return false;
    }
}
