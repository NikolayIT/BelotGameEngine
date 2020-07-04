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
import belote.logic.play.strategy.automat.base.method.BaseMethod;

public class PartnerFirstAttackMaxSuitCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public PartnerFirstAttackMaxSuitCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        Card result = null;
        final Player partner = player.getPartner();
        final Card attackCard = game.getTrickCards().getAttackCard();

        if (attackCard != null && isSecondDefencePosition()) { // partner is the
                                                               // attack player
            final Card maxCard = player.getCards().findMaxSuitCard(attackCard.getSuit());
            if (maxCard != null && !hasPlayerAttack(partner) && isMaxSuitCardLeft(attackCard, false)) {
                result = maxCard;
            }
        }
        return result;
    }
}