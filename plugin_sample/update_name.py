#coding: shift-jis
import clr

from Kbtter import *
from System import *
from TweetSharp import *

from System.Text import *
from System.Text.RegularExpressions import *

class update_name(IKbtterOnTweet,IKbtterOnInitialize):
	def GetPluginName(self):
		return "update_name"
	
	def OnInitialize(self,svc,user):
		#st=SendTweetOptions()
		#st.Status="update_nameを開始しました！"
		#svc.SendTweet(st,DefaultAsyncFunction)
	
	def OnTweet(self,svc,user,st):
		reg=Regex("@"+user.ScreenName+"\\s+update_name\\s+(?<name>.+)\\s?")
		m=reg.Match(st.TextDecoded)
		
		if m.Success :
			print "Rename : "+m.Groups["name"].Value
			op=UpdateProfileOptions()
			op.Name=m.Groups["name"].Value
			svc.UpdateProfile(op)
			tw=SendTweetOptions()
			tw.Status="@" +st.User.ScreenName+ " " +m.Groups["name"].Value+" に改名しました"
			tw.InReplyToStatusId=st.Id
			print tw.Status
			svc.SendTweet(tw,DefaultAsyncFunction)
			fvs=FavoriteTweetOptions()
			fvs.Id=st.Id
			svc.FavoriteTweet(fvs,DefaultAsyncFunction)

def DefaultAsyncFunction(st,res):
	return
