/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumps.noTrumpAttack;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

/**
 * HookCard class. PlayCardMethod which implements the logic of playing a hook card or to take the card (3th defefence position) in
 * defense of color game.
 * @author Dimitar Karamanov
 */
public final class HookCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public HookCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player, final Suit trump) {
        if (trump != null) {
            final boolean teamSuit = isTeamSuit(trump, player.getTeam());
            final boolean noTrumps = isPlayerSuit(player, trump);
            final Card attackCard = game.getTrickCards().getAttackCard();

            if (attackCard != null) {
                if (isThirdDefencePosition()) {
                    final Card max = player.getCards().findMaxSuitCard(attackCard.getSuit());
                    final Card handCard = game.getTrickCards().getHandAttackSuitCard();

                    if (max != null && handCard != null) {
                        if (noTrumps || teamSuit) {
                            if (max != null && isMaxSuitCardLeft(max, false)) {
                                final Card minAboveCard = player.getCards().findMinAboveCard(handCard);
                                if (minAboveCard == null) {
                                    return max;
                                } else {
                                    return player.getCards().getMaxSequenceCardAfter(minAboveCard);
                                }
                            }
                        }

                        if (max.compareTo(handCard) > 0) {
                            return max;
                        }
                    }
                }
            }
        }
        return null;
    }
}