package com.karamanov.beloteGame.gui.screen.base;

import java.io.BufferedWriter;
import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.List;
import java.util.Locale;

import com.karamanov.beloteGame.text.PlayerNameDecorator;
import com.karamanov.beloteGame.text.TextDecorator;

import android.content.Context;
import android.os.Environment;
import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.PackIterator;
import belote.bean.pack.card.Card;
import belote.bean.trick.Trick;
import belote.bean.trick.TrickListIterator;

public final class BeloteLog {

    private BeloteLog() {
        super();
    }

    public static void saveGameInfo(Game game, Context context) {
        List<String> list = new ArrayList<String>();
        TextDecorator textDecorator = new TextDecorator(context);
                
        final ArrayList<String> announceText = textDecorator.getAnnounceText(game.getAnnounceList());
        
        list.add("<html>");
        list.add("<body>");
        list.add("<table align='center'>");
        for (String s : announceText) {
            list.add("<tr><td>");
            list.add(s);
            list.add("</td></tr>");
        }
        list.add("</table>");
        
        
        list.add("<table align='center'>");
        
        for (TrickListIterator iterator = game.getTrickList().iterator(); iterator.hasNext();) { 
            Trick trick = iterator.next(); 
            Player player = trick.getAttackPlayer();
            PlayerNameDecorator playerDecorator = new PlayerNameDecorator(player);
        
            list.add("<tr>");
            
            list.add("<td>");
            list.add(playerDecorator.decorate(context));
            list.add("</td>");
            
            for (PackIterator pi = trick.getTrickCards().iterator(); pi.hasNext();) {
                Card card = pi.next();
                
                list.add("<td>");
                list.add(textDecorator.getRank(card.getRank()) + " " + card.getSuit().getClassShortName());
                String method = card.getCardAcquireMethod() == null ? "You" : card.getCardAcquireMethod();
                list.add(" [" + method + "] ");
                list.add("</td>");
                
            }
            
            list.add("</tr>");
        }
        
        list.add("</table>");
        
        list.add("</body>");
        list.add("</html>");

        saveToFile(list);
    }

    private static void saveToFile(List<String> list) {
        try {
            File root = Environment.getExternalStorageDirectory();

            File dir = new File(root, "belote");
            if (!dir.exists()) {
                dir.mkdir();
            }
            
            if (dir.canWrite()) {

                Calendar calendar = Calendar.getInstance();
                SimpleDateFormat format = new SimpleDateFormat("yyyy_MM_dd_HH_mm_ss", Locale.getDefault());
                String result = format.format(calendar.getTime());

                File file = new File(dir, result + "_log.htm");
                file.createNewFile();

                FileWriter fileWriter = new FileWriter(file);
                try {
                    BufferedWriter out = new BufferedWriter(fileWriter);
                    try {
                        for (String s : list) {
                            out.write(s + "\n");
                        }
                    } finally {
                        out.close();
                    }
                } finally {
                    fileWriter.close();
                }
            }
        } catch (IOException e) { // D.N. }
        }
    }
}
