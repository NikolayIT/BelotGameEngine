package belote.logic.play.strategy.automat.methods.trumps.needless;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitIterator;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

public class MaxCardToPartnerOnDefense extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public MaxCardToPartnerOnDefense(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player, final Suit trump) {
        Card result = null;

        if (isEnemyTeamAnnounce(player) && isTeamSuit(trump, game.getOppositeTeam(player)) && player.getCards().getSize() <= FOUR_CARDS_COUNT && isThirdDefencePosition()) {
            final Card handAttackSuitCard = game.getTrickCards().getHandAttackSuitCard(trump);
            if (handAttackSuitCard != null) {
                final Player partner = player.getPartner();
                final Player handPlayer = game.getPlayerByCard(handAttackSuitCard);
                if (handPlayer != null && handPlayer.equals(partner)) {
                    for (final SuitIterator iterator = Suit.iterator(); iterator.hasNext();) {
                        final Suit suit = iterator.next();
                        if (!suit.equals(trump) && hasTrickAttackSuit(suit)) {
                            final Card max = player.getCards().findMaxSuitCard(suit);
                            if (max != null) {
                                if (result == null || result.compareRankTo(max) < 0) {
                                    result = max;
                                }
                            }
                        }
                    }
                }
            }
        }
        return result;
    }
}