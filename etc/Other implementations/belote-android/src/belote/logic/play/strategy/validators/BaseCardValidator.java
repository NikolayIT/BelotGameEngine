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
 * BaseCardValidator class.
 * @author Dimitar Karamanov
 */
public abstract class BaseCardValidator implements Validatable {

    /**
     * Internal BelotGame instance.
     */
    protected final Game game;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public BaseCardValidator(final Game game) {
        this.game = game;
    }

    /**
     * Returns if there are no other played card yet, false otherwise
     * @param attackCard first play card (may be null if there is no played card yet)
     */
    private final boolean isTrickAttackPlayer(final Card attackCard) {
        return attackCard == null;
    }

    /**
     * Returns true if is bigger and from same suit, false otherwise.
     * @param card first card.
     * @param handCard compare card.
     * @return true if is bigger and from same suit, false otherwise.
     */
    protected boolean isBiggerHandCard(final Card card, final Card handCard) {
        return card.getSuit().equals(handCard.getSuit()) && card.compareRankTo(handCard) > 0;
    }

    /**
     * Returns true if the two cards are from same suit, false otherwise.
     * @param card first card.
     * @param handCard compare card.
     * @return true if the two cards are from same suit, false otherwise.
     */
    protected boolean isSameSuitCard(final Card card, final Card handCard) {
        return card.getSuit().equals(handCard.getSuit());
    }

    /**
     * Returns true if the cards are from same suit and there is no bigger, false otherwise
     * @param player which is checked for played card.
     * @param card played one.
     * @param handCard hand card.
     * @return true if the cards are from same suit and there is no bigger, false otherwise.
     */
    protected boolean isSameSuitCardAndHasNoBigger(final Player player, final Card card, final Card handCard) {
        return card.getSuit().equals(handCard.getSuit()) && player.getCards().findMaxSuitCard(handCard.getSuit()).compareRankTo(handCard) < 0;
    }

    /**
     * Returns true if the cards are from different suit and player has no from the handCard suit, false otherwise.
     * @param player which is checked for played card.
     * @param card played one.
     * @param handCard hand card.
     * @return true if the cards are from different suit and player has no from the handCard suit, false otherwise.
     */
    protected boolean isDifferentSuitAndHasNoFromSuit(final Player player, final Card card, final Card handCard) {
        return !card.getSuit().equals(handCard.getSuit()) && player.getCards().getSuitCount(handCard.getSuit()) == 0;
    }

    /**
     * Validates player card template method which returns true if the player is the first attack player or check for the specific realization in the ancestors
     * classes.
     * @param player which card is validated.
     * @param card provided card.
     * @return boolean true if the card is valid, false otherwise.
     */
    public final boolean validatePlayerCard(final Player player, final Card card) {
        final Card attackCard = game.getTrickCards().getAttackCard();
        if (isTrickAttackPlayer(attackCard)) {
            return true;
        }
        return validateNoAttackPlayerCard(player, card, attackCard);
    }

    /**
     * Returns if the provided player has couple.
     * @param player provided player.
     * @param card provided card.
     * @return boolean true if has a couple false otherwise.
     */
    public final boolean hasPlayerCouple(final Player player, final Card card) {
        if (card.isBeloteCard() && player.getCards().hasCouple(card)) {
            return hasPlayerCouple(card);
        }
        return false;
    }

    /**
     * Returns if the provided card is a couple card.
     * @param card provided card.
     * @return boolean true if the card is from a couple false otherwise.
     */
    protected abstract boolean hasPlayerCouple(final Card card);

    /**
     * Validates player card when the player is not attack.
     * @param player provided player.
     * @param card provided card.
     * @param attackCard attack card.
     * @return boolean true if the card is valid, false otherwise.
     */
    protected abstract boolean validateNoAttackPlayerCard(final Player player, final Card card, final Card attackCard);
}