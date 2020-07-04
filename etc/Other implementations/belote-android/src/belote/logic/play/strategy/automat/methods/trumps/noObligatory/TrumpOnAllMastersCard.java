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
 * TrumpOnAllMastersCard class. PlayCardMethod which implements the logic of playing a card when the player or his partner are trump keeper and
 * all his cards are master cards - play trump card and get all other hands.
 * @author Dimitar Karamanov
 */
public final class TrumpOnAllMastersCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public TrumpOnAllMastersCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    protected Card getPlayMethodCard(final Player player, final Suit trump) {
        // if all cards are master and is trump keeper or team suit play trump.
        if (trump != null && (isPlayerSuit(player, trump) || isTeamSuit(trump, player.getTeam())) && isAllCardsMasters(player)) {
            final Card minTrumpCard = player.getCards().findMinSuitCard(trump);
            if (minTrumpCard != null) {
                return minTrumpCard;
            }
        }
        return null;
    }
}