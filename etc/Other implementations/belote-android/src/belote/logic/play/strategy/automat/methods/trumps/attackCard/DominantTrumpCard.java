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
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

/**
 * DominantTrumpCard class. PlayCardMethod which implements the logic of playing a dominant card trump suit in color game.
 * @author Dimitar Karamanov
 */
public final class DominantTrumpCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public DominantTrumpCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player, final Suit trump) {
        if (trump != null && player.equals(game.getAnnounceList().getOpenContractAnnouncePlayer()) && !isTeamSuit(trump, player.getTeam())) {
            final int count = player.getCards().getSuitCount(trump);
            final int passCount = getPassedSuitCardsCount(trump);
            final int leftCount = Rank.getRankCount() - passCount - count;

            if ((count > TWO_CARDS_COUNT && passCount == 0) || (count >= leftCount && leftCount > 0)) {
                final Card max = player.getCards().findMaxSuitCard(trump);
                if (max != null && isMaxSuitCardLeft(max, true)) {
                    return max;
                }
            }
        }
        return null;
    }
}