/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumps.attackCard;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.bean.trick.Trick;
import belote.bean.trick.TrickListIterator;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

/**
 * TrumpToPartnerCard class. PlayCardMethod which implements the logic of playing a trump card if the partner declared the color game.
 * @author Dimitar Karamanov
 */
public final class TrumpToPartnerCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public TrumpToPartnerCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player, final Suit trump) {
        final Player partner = player.getPartner();
        if (trump != null && partner.equals(game.getAnnounceList().getOpenContractAnnouncePlayer()) && !isTeamSuit(trump, player.getTeam())
                && !hasPlayerSuitAttack(player, trump) && !(player.getCards().getSize() <= TWO_CARDS_COUNT)) {
            final Card card = player.getCards().findMaxSuitCard(trump);
            if (card != null && isMaxSuitCardLeft(card, false)) {
                return card;
            }
            return player.getCards().findMinSuitCard(trump);
        }
        return null;
    }

    /**
     * Returns true if the provided player has attack with card with provided suit, false otherwise
     * @param player provided player
     * @param suit provided suit
     * @return boolean true if the provided player has attack with card with provided suit, false otherwise
     */
    private boolean hasPlayerSuitAttack(final Player player, final Suit suit) {
        for (final TrickListIterator iterator = game.getTrickList().iterator(); iterator.hasNext();) {
            final Trick trick = iterator.next();

            final Player attackPlayer = trick.getAttackPlayer();
            final Card attackCard = trick.getPlayerCard(attackPlayer);

            if (attackPlayer.equals(player) && attackCard.getSuit().equals(suit)) {
                return true;
            }
        }
        return false;
    }
}