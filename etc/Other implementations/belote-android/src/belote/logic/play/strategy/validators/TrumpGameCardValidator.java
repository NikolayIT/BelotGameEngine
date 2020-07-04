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
import belote.bean.announce.Announce;
import belote.bean.announce.AnnounceUnit;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;

/**
 * TrumpGameCardValidator class.
 * @author Dimitar Karamanov
 */
public class TrumpGameCardValidator extends BaseCardValidator {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public TrumpGameCardValidator(final Game game) {
        super(game);
    }

    /**
     * Validates player card.
     * @param player provided player.
     * @param card provided card.
     * @return boolean true if the card is valid, false otherwise.
     */
    public boolean validateNoAttackPlayerCard(final Player player, final Card card, final Card attackCard) {
        // The attack card is from trump suit
        if (attackCard.getSuit().equals(getTrumpSuit())) {
            return validatePlayerCardTrumpAttack(player, card, attackCard);
        } else {
            return validatePlayerCardNoTrumpAttack(player, card, attackCard);
        }
    }

    /**
     * Returns if the provided card is a couple card.
     * @param card provided card.
     * @return boolean true if the card is from a couple false otherwise.
     */
    protected final boolean hasPlayerCouple(Card card) {
        return card.getSuit().equals(getTrumpSuit());
    }

    /**
     * Validates player card on trump attack card.
     * @param player provided player.
     * @param card provided card.
     * @param attackCard attack card.
     * @return boolean true if the card is valid, false otherwise.
     */
    private boolean validatePlayerCardTrumpAttack(Player player, Card card, Card attackCard) {
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
     * Validates player card on no trump attack card.
     * @param player provided player.
     * @param card provided card.
     * @param attackCard attack card.
     * @return boolean true if the card is valid, false otherwise.
     */
    private boolean validatePlayerCardNoTrumpAttack(final Player player, final Card card, final Card attackCard) {
        if (isSameSuitCard(card, attackCard)) {
            return true;
        }
        if (!player.getCards().hasSuitCard(attackCard.getSuit())) {
            // Return true if the player has no trump card
            if (!player.getCards().hasSuitCard(getTrumpSuit())) {
                return true; // Check if is played trump card yet (somebody had
                             // played trump card before the player)
            }
            if (game.getTrickCards().hasSuitCard(getTrumpSuit())) {
                if (validatePlayBiggerTrumpCard(player, card, attackCard)) {
                    return true;
                }
            } else {
                if (validatePlayTrumpCard(player, card, attackCard)) {
                    return true;
                }
            }
        }

        return false;
    }

    /**
     * Validates player when have to play bigger trump card if has or other.
     * @param player provided player.
     * @param card provided card.
     * @param attackCard attack card.
     * @return boolean true if the card is valid, false otherwise.
     */
    private boolean validatePlayBiggerTrumpCard(final Player player, final Card card, final Card attackCard) {

        final Card maxTrumpCard = game.getTrickCards().findMaxSuitCard(getTrumpSuit());
        final Player handPlayer = game.getPlayerByCard(maxTrumpCard);

        if (player.isSameTeam(handPlayer)) {
            return true;
        }
        if (card.isSameSuitBiggerCard(maxTrumpCard)) {
            return true;
        }
        if (player.getCards().findMaxSuitCard(getTrumpSuit()).compareRankTo(maxTrumpCard) < 0) {
            return true;
        }
        return false;
    }

    /**
     * Validates player card when have to play obligatory trump card if has.
     * @param player provided player.
     * @param card provided card.
     * @param attackCard attack card.
     * @return boolean true if the card is valid, false otherwise.
     */
    private boolean validatePlayTrumpCard(final Player player, final Card card, final Card attackCard) {
        if (card.getSuit().equals(getTrumpSuit())) {
            return true;
        }

        Player handPlayer = game.getPlayerByCard(game.getTrickCards().getHandAttackSuitCard());
        if (player.isSameTeam(handPlayer)) {
            return true;
        }
        return false;
    }

    /**
     * Returns trump suit.
     * @return Suit trump suit.
     */
    private Suit getTrumpSuit() {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        return AnnounceUnit.transformFromAnnounceSuitToSuit(announce.getAnnounceSuit());
    }
}
