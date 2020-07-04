package com.karamanov.beloteGame.gui.screen.main.dealer;

import java.util.ArrayList;

import android.content.Intent;
import android.graphics.Canvas;
import android.view.View;
import belote.base.BelotException;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.rank.Rank;

import com.karamanov.beloteGame.Belote;
import com.karamanov.beloteGame.gui.screen.gameResume.GameResumeActivity;
import com.karamanov.beloteGame.gui.screen.main.BeloteActivity;
import com.karamanov.beloteGame.gui.screen.main.BeloteView;
import com.karamanov.beloteGame.gui.screen.main.message.MessageData;
import com.karamanov.framework.MessageActivity;
import com.karamanov.framework.graphics.Rectangle;

final class PlayDealer extends BaseDealer {

    public PlayDealer(MessageActivity context, BeloteView belotPanel, View buttons) {
        super(context, belotPanel, buttons);
    }

    @Override
    public void checkKeyPressed(int keyCode) {
        if (keyCode == BeloteActivity.NAV_PRESS) {
            final boolean isPlayedCard = processPlaySelectedCard();

            // Don't process game playing after human has played and trick is ended.
            if (!isPlayedCard || !beloteFacade.isTrickEnd()) {
                processGamePlaying(keyCode);
            }
        } else if (keyCode == BeloteActivity.NAV_LEFT && beloteFacade.isHumanTrickOrder()) {
            final Card card = beloteFacade.selectNextLeftCard();
            processSelectCard(card);
        } else if (keyCode == BeloteActivity.NAV_RIGHT && beloteFacade.isHumanTrickOrder()) {
            final Card card = beloteFacade.selectNextRightCard();
            processSelectCard(card);
        } else {
            processGamePlaying(keyCode);
        }
    }

    /**
     * Checks double click card.
     */
    private boolean processPlaySelectedCard() {
        if (beloteFacade.isPlayingGameMode() && beloteFacade.isHumanTrickOrder() && beloteFacade.getHumanPlayer().getSelectedCard() != null) {
            Player player = beloteFacade.getHumanPlayer();

            if (beloteFacade.validatePlayerCard(player, player.getSelectedCard())) {
                // couples, preferred, unwanted and missed suit
                beloteFacade.processHumanPlayerCard(player, beloteFacade.getHumanPlayer().getSelectedCard());
                // repaint frame
                invalidateGame();

                ArrayList<MessageData> messages = getMessageList(player, player.getSelectedCard());

                if (messages.size() > 0) {
                    displayMessage(player, messages);
                } else {
                    sleep(PLAY_DELAY);
                }

                return true;
            }
        }

        return false;
    }

    /**
     * Process game playing.
     * 
     * @param keyCode pressed key code.
     * @param gameAction status.
     */
    private void processGamePlaying(int keyCode) {
        checkTrickEnd();

        if (beloteFacade.checkGameEnd()) {
            endGame();
        } else {
            processRoundPlaying(keyCode);
        }
    }

    /**
     * Process round playing.
     * 
     * @param keyCode pressed key code.
     * @param gameAction status.
     */
    private void processRoundPlaying(int keyCode) {
        if (!beloteFacade.isHumanTrickOrder()) {
            if (beloteFacade.getHumanTrickCard() == null) {
                processPlayTillHumanPlayer(keyCode);
            } else {
                processPlayAfterHumanPlayer(keyCode);
            }
        }
    }

    /**
     * Process rounds after human player.
     * 
     * @param keyCode
     * @param gameAction
     */
    private void processPlayAfterHumanPlayer(int keyCode) {
        beloteFacade.getHumanPlayer().setSelectedCard(null);
        playRepeatedSingleRoundAfterHumanPlayer();
    }

    /**
     * Process rounds till human player.
     * 
     * @param keyCode
     * @param gameAction
     */
    private void processPlayTillHumanPlayer(int keyCode) {
        if (beloteFacade.getHumanPlayer().equals(beloteFacade.getGame().getTrickAttackPlayer())) {
            selectHumanSingleCard();
            processSelectCard(keyCode);
        } else {
            playRepeatedSingleRoundTillHumanPlayer();
            selectHumanSingleCard();
        }
    }

    /**
     * Checks card click.
     * 
     * @param keyCode pressed key code.
     * @param gameAction status.
     */
    private void processSelectCard(int keyCode) {
        boolean keyLeftRightAction = keyCode == BeloteActivity.NAV_LEFT || keyCode == BeloteActivity.NAV_RIGHT;
        if (beloteFacade.isPlayingGameMode() && beloteFacade.isHumanTrickOrder() && keyLeftRightAction) {
            Card card = null;

            if (keyCode == BeloteActivity.NAV_LEFT) {
                card = beloteFacade.selectNextLeftCard();
            }

            if (keyCode == BeloteActivity.NAV_RIGHT) {
                card = beloteFacade.selectNextRightCard();
            }

            processSelectCard(card);
        }
    }

    @Override
    public void checkPointerPressed(float x, float y) {
        boolean isPlayedCard = processPlaySelectedCard(getHumanCardUnderPointer(x, y));

        if (!isPlayedCard) {
            isPlayedCard = processSelectHumanCard(x, y);
        }

        if (!isPlayedCard || !beloteFacade.isTrickEnd()) {
            checkTrickEnd();

            if (beloteFacade.checkGameEnd()) {
                endGame();
            } else {
                processRoundPlay(x, y);
            }
        }
    }

    private Card getHumanCardUnderPointer(final float x, final float y) {
        Player player = beloteFacade.getHumanPlayer();

        Canvas canvas = belotPanel.getBufferedCanvas();
        if (canvas != null) {
            for (int i = 0; i < Rank.getRankCount(); i++) {
                if (i < player.getCards().getSize()) {

                    Rectangle rec = belotPainter.getPlayerCardRectangle(canvas, beloteFacade, i, player);
                    Card card = player.getCards().getCard(i);

                    if (rec.include((int) x, (int) y)) {
                        return card;
                    }
                }
            }
        }
        return null;
    }

    /**
     * Checks double click card.
     */
    private boolean processPlaySelectedCard(final Card card) {
        if (beloteFacade.isPlayingGameMode() && beloteFacade.isHumanTrickOrder() && beloteFacade.getHumanPlayer().getSelectedCard() != null
                && beloteFacade.getHumanPlayer().getSelectedCard().equals(card)) {
            final Player player = beloteFacade.getHumanPlayer();

            if (beloteFacade.validatePlayerCard(player, beloteFacade.getHumanPlayer().getSelectedCard())) {
                // couples, preferred, unwanted and missed suit
                beloteFacade.processHumanPlayerCard(player, player.getSelectedCard());
                // repaint frame
                invalidateGame();
                // check for display message
                ArrayList<MessageData> messages = getMessageList(player, beloteFacade.getHumanPlayer().getSelectedCard());
                if (messages.size() > 0) {
                    displayMessage(player, messages);
                } else {
                    sleep(PLAY_DELAY);
                }

                return true;
            }
        }

        return false;
    }

    /**
     * Checks card click.
     * 
     * @param keyCode pressed key code.
     * @param gameAction status.
     * @return if a card was selected.
     */
    private boolean processSelectHumanCard(float x, float y) {
        if (beloteFacade.isPlayingGameMode() && beloteFacade.isHumanTrickOrder()) {
            Card card = getHumanCardUnderPointer(x, y);
            processSelectCard(card);
            return true;
        }
        return false;
    }

    /**
     * Process the selected card.
     * 
     * @param card selected one.
     */
    private void processSelectCard(final Card card) {
        final Player player = beloteFacade.getHumanPlayer();

        if (card != null && beloteFacade.validatePlayerCard(player, card)) {
            if (!card.equals(beloteFacade.getHumanPlayer().getSelectedCard())) {
                beloteFacade.getHumanPlayer().setSelectedCard(card);
                invalidateGame();
            }
        }
    }

    /**
     * Checks for round end.
     */
    private void checkTrickEnd() {
        if (beloteFacade.isTrickEnd()) {
            beloteFacade.processTrickData();
            invalidateGame();
        }
    }

    /**
     * End game.
     */
    private void endGame() {
        Belote belote = (Belote) context.getApplication();
        belote.getMessageProcessor().stopMessaging();

        beloteFacade.processTrickData();
        beloteFacade.calculateTeamsPoints();

        if (beloteFacade.getGame().getAnnounceList().getOpenContractAnnounce() != null) {

            handler.post(new Runnable() {
                public void run() {
                    Intent intent = new Intent(context, GameResumeActivity.class);
                    intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                    context.startActivity(intent);
                }
            });
        }
    }

    /**
     * Process round playing.
     * @param keyCode pressed key code.
     * @param gameAction status.
     */
    private void processRoundPlay(float x, float y) {
        if (!beloteFacade.isHumanTrickOrder()) {
            if (beloteFacade.getHumanTrickCard() == null) {
                processTillHumanPlayer(x, y);
            } else {
                processAfterHumanPlayer(x, y);
            }
        }
    }

    /**
     * Process rounds till human player.
     * 
     * @param keyCode
     * @param gameAction
     */
    private void processTillHumanPlayer(float x, float y) {
        if (beloteFacade.getHumanPlayer().equals(beloteFacade.getGame().getTrickAttackPlayer())) {
            selectHumanSingleCard();
            processSelectHumanCard(x, y);
        } else {
            playRepeatedSingleRoundTillHumanPlayer();
            selectHumanSingleCard();
        }
    }

    /**
     * Selects human single card.
     */
    private void selectHumanSingleCard() {
        final Player player = beloteFacade.getHumanPlayer();
        if (beloteFacade.isPlayingGameMode() && beloteFacade.isHumanTrickOrder() && player.getCards().getSize() == 1) {
            final Card card = player.getCards().getFirstNoNullCard();
            processSelectCard(card);
        }
    }

    /**
     * Process rounds after human player.
     * 
     * @param keyCode
     * @param gameAction
     */
    private void processAfterHumanPlayer(float x, float y) {
        beloteFacade.getHumanPlayer().setSelectedCard(null);
        playRepeatedSingleRoundAfterHumanPlayer();
    }

    /**
     * Plays one round till human player.
     */
    private void playRepeatedSingleRoundTillHumanPlayer() {
        for (Player player = beloteFacade.getGame().getTrickAttackPlayer(); !player.equals(beloteFacade.getHumanPlayer()); player = beloteFacade
                .getPlayerAfter(player)) {
            playSingleRoundPlayerCard(player);
        }
    }

    /**
     * Plays one round after human player (Till end of round).
     */
    private void playRepeatedSingleRoundAfterHumanPlayer() {
        for (Player player = beloteFacade.getPlayerAfter(beloteFacade.getHumanPlayer()); !player.equals(beloteFacade.getGame().getTrickAttackPlayer()); player = beloteFacade
                .getPlayerAfter(player)) {
            playSingleRoundPlayerCard(player);
        }
    }

    private void playSingleRoundPlayerCard(final Player player) {
        try {
            Card card = beloteFacade.playSingleHand(player);
            invalidateGame();

            if (card != null) {
                ArrayList<MessageData> messages = getMessageList(player, card);
                if (messages.size() > 0) {
                    displayMessage(player, messages);
                } else {
                    sleep(PLAY_DELAY);
                }
            }

        } catch (BelotException be) {
        }
    }
}
