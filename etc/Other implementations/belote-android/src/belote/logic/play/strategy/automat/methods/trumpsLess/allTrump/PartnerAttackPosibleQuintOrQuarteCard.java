package belote.logic.play.strategy.automat.methods.trumpsLess.allTrump;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

public class PartnerAttackPosibleQuintOrQuarteCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public PartnerAttackPosibleQuintOrQuarteCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        final Card handAttackSuitCard = game.getTrickCards().getHandAttackSuitCard();
        if (handAttackSuitCard != null) {
            final Player partner = player.getPartner();
            final Player handPlayer = game.getPlayerByCard(handAttackSuitCard);
            if (handPlayer != null && handPlayer.equals(partner) && isMaxSuitCardLeft(handAttackSuitCard, false)) {
                if (isPartnerPossibleQuintOrQuarteSuit(handAttackSuitCard.getSuit(), player)) {
                    return player.getCards().findMaxSuitCard(handAttackSuitCard.getSuit());
                }
            }
        }
        return null;
    }
}