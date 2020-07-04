/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitIterator;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * PartnerPreferredSuitCard class. PlayCardMethod which implements the logic of playing card from partner preferred and not missed suit.
 * @author Dimitar Karamanov.
 */
public final class PartnerPreferredSuitCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public PartnerPreferredSuitCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    protected Card getPlayMethodCard(final Player player) {
        final Player partner = player.getPartner();
        final Suit trump = getTrump();
        // Prefer suits
        for (final SuitIterator iterator = partner.getPreferredSuits().iterator(); iterator.hasNext();) {
            final Suit suit = iterator.next();
            if ((trump == null || !trump.equals(suit)) && !partner.getMissedSuits().contains(suit)) {
                final Card card = player.getCards().findMinSuitCard(suit);
                if (card != null) {
                    return card;
                }
            }
        }
        return null;
    }
}