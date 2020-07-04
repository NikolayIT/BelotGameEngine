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
import belote.logic.play.strategy.automat.base.PlayCardMethod;
import belote.logic.play.strategy.automat.base.method.BaseMethod;
import belote.logic.play.strategy.automat.methods.MinMeterSuitCard;

/**
 * MinimumOfAllMastersCard class. PlayCardMethod which implements the logic of playing the minimum by rank card when all cards are master.
 * @author Dimitar Karamanov.
 */
public final class MinimumOfAllMastersCard extends BaseMethod {

    /**
     * Play card method helper.
     */
    private final PlayCardMethod minMeterSuitCard;

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public MinimumOfAllMastersCard(final Game game) {
        super(game);

        minMeterSuitCard = new MinMeterSuitCard(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        if (isAllCardsMasters(player)) {
            return minMeterSuitCard.getPlayerCard(player);
        }
        return null;
    }
}