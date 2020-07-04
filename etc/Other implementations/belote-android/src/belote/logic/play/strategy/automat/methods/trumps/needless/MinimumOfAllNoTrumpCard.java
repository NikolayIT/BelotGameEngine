/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumps.needless;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

/**
 * MinimumOfAllNoTrumpCard class. PlayCardMethod which implements the logic of playing the minimum by rank suit card in color game (excludes the trump's cards).
 * @author Dimitar Karamanov
 */
public final class MinimumOfAllNoTrumpCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public MinimumOfAllNoTrumpCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player, final Suit trump) {
        return player.getCards().findMinAllCard(trump);
    }
}