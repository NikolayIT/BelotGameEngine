/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumps.obligatoryTrump;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.PlayCardMethod;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

/**
 * TeamAnnounceSafeTrumpCard class. PlayCardMethod which implements the logic of playing a safe trump card if is player team announce.
 * @author Dimitar Karamanov
 */
public final class TeamAnnounceSafeTrumpCard extends BaseTrumpMethod {

    /**
     * Helper method.
     */
    private final PlayCardMethod safeTrumpCardMethod;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public TeamAnnounceSafeTrumpCard(final Game game) {
        super(game);
        safeTrumpCardMethod = new SafeTrumpCard(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    protected Card getPlayMethodCard(final Player player, final Suit trump) {
        Card result = null;
        if (isPlayerTeamAnnounce(player)) {
            result = safeTrumpCardMethod.getPlayerCard(player);
        }
        return result;
    }
}