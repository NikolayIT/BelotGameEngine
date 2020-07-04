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
import belote.logic.play.strategy.automat.base.PlayCardMethod;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;
import belote.logic.play.strategy.automat.methods.MinMeterSuitCard;

/**
 * MinimumOfAllMastersCards class. PlayCardMethod which implements the logic of playing when all user's cards are master's in a color game.
 * @author Dimitar Karamanov
 */
public final class MinimumOfAllMastersCards extends BaseTrumpMethod {

    /**
     * PlayCardMethod method used as part of this one (composition).
     */
    private final PlayCardMethod minMeterSuitCard;

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public MinimumOfAllMastersCards(final Game game) {
        super(game);
        minMeterSuitCard = new MinMeterSuitCard(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player, final Suit trump) {
        if (isAllCardsMasters(player)) {
            return minMeterSuitCard.getPlayerCard(player);
        }
        return null;
    }
}