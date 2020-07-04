/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.validators;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;

/**
 * AllTrumpCardValidator class.
 * @author Dimitar Karamanov
 */
public class AllTrumpCardValidator extends BaseCardValidator {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public AllTrumpCardValidator(final Game game) {
        super(game);
    }

    /**
     * Validates player card.
     * @param player provided player.
     * @param card provided card.
     * @return boolean true if the card is valid, false otherwise.
     */
    public boolean validateNoAttackPlayerCard(final Player player, final Card card, final Card attackCard) {
        final Card handCard = game.getTrickCards().findMaxSuitCard(attackCard.getSuit());

        if (isBiggerHandCard(card, handCard)) {
            return true;
        }
        if (isSameSuitCardAndHasNoBigger(player, card, handCard)) {
            return true;
        }
        if (isDifferentSuitAndHasNoFromSuit(player, card, handCard)) {
            return true;
        }
        return false;
    }

    /**
     * Returns if the provided card is a couple card.
     * @param card provided card.
     * @return boolean true if the card is from a couple false otherwise.
     */
    protected final boolean hasPlayerCouple(Card card) {
        final Card attack = game.getTrickCards().getAttackCard();
        if (attack == null || attack.getSuit().equals(card.getSuit())) {
            return true;
        }
        return false;
    }
}