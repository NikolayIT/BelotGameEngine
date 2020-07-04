/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumps.noObligatory;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

/**
 * EnemyGameSingleTrumpCard class. PlayCardMethod which implements the logic of playing a card when the game is declared by enemy team and the
 * player has only single trump card - play it.
 * @author Dimitar Karamanov
 */
public final class EnemyGameSingleTrumpCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public EnemyGameSingleTrumpCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    protected Card getPlayMethodCard(final Player player, final Suit trump) {
        final Card maxTrumpCard = player.getCards().findMaxSuitCard(trump);
        final int count = player.getCards().getSuitCount(trump);
        // if have one no master card better play it (the enemy can get it)
        if (maxTrumpCard != null && count == SINGLE_CARD_COUNT && !isPlayerTeamAnnounce(player) && !isMaxSuitCardLeft(maxTrumpCard, true)) {
            return maxTrumpCard;
        }
        return null;
    }
}