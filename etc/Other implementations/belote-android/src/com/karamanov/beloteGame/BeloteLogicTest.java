package com.karamanov.beloteGame;

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;

import android.os.Environment;
import android.util.Log;
import belote.base.BelotException;
import belote.bean.GameMode;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.bean.pack.card.Card;
import belote.logic.BeloteFacade;

public class BeloteLogicTest {
    /**
     * Test games count
     */
    private static final int TEST_GAMES_COUNT = 1000;

    /**
     * Counter
     */
    private static int gameCount = 0;

    // private final static ArrayList<String> log = new ArrayList<String>();

    /**
     * Start test point
     * @param args
     */
    public static void test() {

        try {
            File root = Environment.getExternalStorageDirectory();
            if (root.canWrite()) {
                File file = new File(root, "test.txt");
                file.createNewFile();
                FileWriter fileWriter = new FileWriter(file);
                fileWriter.write("--------------------------------------------------------------------------------");
                fileWriter.flush();
                fileWriter.close();
            }
        } catch (IOException e) {
            // D.N.
        }

        addLog("--------------------------------------------------------------------------------");

        BeloteFacade game = new BeloteFacade();
        game.newGame();

        while (gameCount < TEST_GAMES_COUNT) {
            try {
                processPlaying(game);
            } catch (Exception ex) {
                System.out.println(ex.toString());
                System.out.println("---------------------------------------------------------------");
                ex.printStackTrace();
            }
        }

        addLog("--------------------------------------------------------------------------------");
    }

    private static void newAnnounceDeal(BeloteFacade game) {
        game.processTrickData();
        game.setNextDealAttackPlayer();
        saveLogInfo(game);
        game.newGame();
    }

    private static void processPlaying(BeloteFacade game) throws BelotException {
        if (game.isPlayingGameMode()) {
            processGamePlaying(game);
        } else {
            processAnnounceDeal(game);
        }
    }

    private static void processAnnounceDeal(BeloteFacade game) {
        if (!game.isAnnounceGameMode() && !game.isPlayingGameMode()) {
            newAnnounceDeal(game);
        } else if (game.canDeal()) {
            processSingleAnnounceDeal(game);
        } else {
            if (game.getGame().getAnnounceList().getContractAnnounce() == null) {
                newAnnounceDeal(game);
            } else {
                newGame(game);
            }
        }
    }

    private static void processSingleAnnounceDeal(BeloteFacade game) {
        game.processNextAnnounce();
    }

    private static void processGamePlaying(BeloteFacade game) throws BelotException {
        checkRoundEnd(game);
        if (game.checkGameEnd()) {
            endGame(game);
        } else {
            playOneRound(game);
        }

    }

    /**
     * Checks for round end.
     */
    private static void checkRoundEnd(BeloteFacade game) {
        if (game.isTrickEnd()) {
            addLog("New Round");
            game.processTrickData();
        }
    }

    private static void newGame(BeloteFacade game) {
        game.setGameMode(GameMode.PlayGameMode);
        game.manageRestCards();
    }

    private static void endGame(BeloteFacade game) {
        game.processTrickData();
        game.calculateTeamsPoints();
    }

    private static void playOneRound(BeloteFacade game) throws BelotException {
        Player player = game.getGame().getTrickAttackPlayer();

        for (int i = 0; i < game.getGame().getPlayersCount(); i++) {
            addLog("Player -> " + player.getID() + " see logic");

            Card card = game.playSingleHand(player);

            addLog("Player -> " + player.getID() + " Card -> " + card.toString());

            if (game.hasPlayerCouple(player, card)) {
                game.setPlayerCouple(player, card.getSuit());
            }

            player = game.getPlayerAfter(player);
        }
    }

    private static void saveLogInfo(BeloteFacade game) {
        // System.out.println("saveLogInfo");
        Announce announce = game.getGame().getAnnounceList().getContractAnnounce();
        if (announce != null) {
            gameCount++;

            /*
             * StringBuffer sb = new StringBuffer(8000);
             * 
             * Pack[] packs = new Pack[game.getPlayersCount()]; for (int i = 0; i < game.getPlayersCount(); i++) { packs[i] = new Pack(); for (TrickListIterator
             * rli = game.getTrickList().iterator(); rli.hasNext();) { Trick trick = rli.next(); packs[i].add(trick.getPlayerCard(game.getPlayer(i))); }
             * 
             * if (announce.getAnnounceSuit().equals(AnnounceSuit.AllTrump)) { packs[i].arrangeAT(); } if
             * (announce.getAnnounceSuit().equals(AnnounceSuit.NotTrump)) { packs[i].arrangeNT(); } if (announce.isColorAnnounce()) { packs[i]
             * .arrangeCL(AnnounceUnit.transformFromAnnounceSuitToSuit(announce .getAnnounceSuit())); } }
             * 
             * String count = "" + gameCount; int n = 7 - count.length(); for (int i = 0; i < n; i++) { count = "0" + count; }
             * 
             * // String fileName = "LOG/game_" + count + "_" + announce.toString() + ".htm";
             * 
             * sb.append("<html>"); sb.append("<body>"); sb.append("<table width = '100%'>"); sb.append("<tr><td>"); //sb.append("<b>" +
             * game.getAnnounceList().get .getAnnounceText() + "</b>"); sb.append("</td></tr>"); sb.append("</table>");
             * 
             * sb.append("<br>"); sb.append("<table width = '100%' border='1' bgcolor = '#FFE6CC'>" ); sb.append("<tr>"); for (int i = 0; i <
             * game.getPlayersCount(); i++) { sb.append("<td width='25%'>"); sb.append("<b>" + game.getPlayer(i).toString() + "</b> "); sb.append("</td>"); }
             * sb.append("</tr>"); sb.append("<tr>"); for (int i = 0; i < game.getPlayersCount(); i++) { sb.append("<td width='25%'>"); sb.append("<b>P: " +
             * game.getPlayer(i).getPreferredSuits().toHTMLString() + "</b> "); sb.append("</td>"); } sb.append("</tr>"); sb.append("<tr>"); for (int i = 0; i <
             * game.getPlayersCount(); i++) { sb.append("<td width='25%'>"); sb.append("<b>U: " + game.getPlayer(i).getUnwantedSuits().toHTMLString() +
             * "</b> "); sb.append("</td>"); } sb.append("</tr>"); sb.append("<tr>"); for (int i = 0; i < game.getPlayersCount(); i++) {
             * sb.append("<td width='25%'>"); sb.append("<b>M: " + game.getPlayer(i).getMissedSuits().toHTMLString() + "</b> "); sb.append("</td>"); }
             * sb.append("</tr>"); sb.append("</table>");
             * 
             * for (TrickListIterator rli = game.getTrickList().iterator(); rli.hasNext();) { Trick round = rli.next(); Player player = round.getAttackPlayer();
             * 
             * sb.append("<br>"); sb.append("<table width = '100%' border='1' bgcolor = '#FFE6CC'>" );
             * 
             * sb.append("<tr>"); for (int i = 0; i < game.getPlayersCount(); i++) { sb.append("<td width='25%'>"); sb.append("<b>" + player.toString() +
             * "</b> "); // sb.append(packs[player.getID()].toHTMLString()); sb.append("</td>");
             * 
             * packs[player.getID()].remove(round.getPlayerCard(player));
             * 
             * player = game.getPlayerAfter(player); } sb.append("</tr>");
             * 
             * sb.append("<tr>"); for (PackIterator pi = round.iterator(); pi.hasNext();) { Card card = pi.next(); sb.append("<td width='25%'>");
             * sb.append("<b>" + card.getCardAcquireMethod() + "</b>"); sb.append("</td>"); } sb.append("</tr>");
             * 
             * sb.append("<tr>"); for (PackIterator pi = round.iterator(); pi.hasNext();) { Card card = pi.next(); sb.append("<td width='25%'>");
             * sb.append("<b>" + card.toString() + "</b>"); sb.append(" - "); sb.append(card.getRank().getRankSign()); sb.append(" ");
             * sb.append(card.getSuit().getSuitImageTag()); sb.append("</td>"); } sb.append("</tr>");
             * 
             * sb.append("</table>"); }
             * 
             * sb.append("</body>"); sb.append("</html>");
             */
            // Belot.addLog("Success !");
            // SimpleDateFormat f = new SimpleDateFormat("HH:mm:ss");
            // String s = f.format(new Date()) + " -> Success \n";
            // log.add(s);
        }
    }

    public static void addLog(String string) {
        String gc = String.valueOf(gameCount);
        while (gc.length() < 4) {
            gc = "0" + gc;
        }
        Log.i("BelotLogger " + gc, string);
        /*
         * try { File root = Environment.getExternalStorageDirectory(); if (root.canWrite()){ File file = new File(root, "test.txt"); if (!file.exists()) {
         * file.createNewFile(); } FileWriter fileWriter = new FileWriter(file, true);
         * 
         * SimpleDateFormat f = new SimpleDateFormat("HH:mm:ss"); String gc = String.valueOf(gameCount);
         * 
         * while (gc.length() < 3) { gc = "0" + gc; }
         * 
         * fileWriter.write(gc + " [" + f.format(new Date()) + "] " + string + "\n");
         * 
         * fileWriter.flush(); fileWriter.close(); } } catch (IOException e) { //D.N. }
         */
    }
}
