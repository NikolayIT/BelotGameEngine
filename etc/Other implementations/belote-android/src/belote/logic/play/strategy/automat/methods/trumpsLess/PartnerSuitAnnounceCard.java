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

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitIterator;
import belote.bean.pack.card.suit.SuitList;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * PartnerSuitAnnounceCard class. PlayCardMethod which implements the logic of playing card from suit declared by partner during game announce.
 * @author Dimitar Karamanov
 */
public final class PartnerSuitAnnounceCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public PartnerSuitAnnounceCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        final Player partner = player.getPartner();
        
        final SuitList suits = getTrumpAnnounces(partner);
        
        for (final SuitIterator iterator = suits.iterator(); iterator.hasNext();) {
            Suit suit = iterator.next();
            
            Card result;
            result = player.getCards().findMaxSuitCard(suit);
            if (result != null && isMaxSuitCardLeft(result, false)) {
                return result;
            }

            result = player.getCards().findMaxSuitCard(suit);
            if (result != null) {
                return result;
            }
        }
        
        return null;
    }
}