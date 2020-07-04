/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.base.method;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.bean.announce.AnnounceUnit;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;

/**
 * BaseTrumpMethod class. Based class of all AI methods used in color game. Returns null card if are called during no color game.
 * @author Dimitar Karamanov
 */
public abstract class BaseTrumpMethod extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public BaseTrumpMethod(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public final Card getPlayMethodCard(final Player player) {
        final Announce announce = game.getAnnounceList().getContractAnnounce();

        if (announce != null && announce.isTrumpAnnounce()) {
            final Suit trump = AnnounceUnit.transformFromAnnounceSuitToSuit(announce.getAnnounceSuit());
            return getPlayMethodCard(player, trump);
        } else {
            return null;
        }
    }

    /**
     * Returns player's card for trump game.
     * @param player who is on turn.
     * @param trump suit.
     * @return player's card for trump game.
     */
    protected abstract Card getPlayMethodCard(final Player player, final Suit trump);
}
