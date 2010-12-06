using System;
using System.Collections.Generic;
using System.Text;
using Sina.Api;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using Sinawler.Model;
using System.Data;

namespace Sinawler
{
    class UserTagRobot : RobotBase
    {
        private UserQueue queueUserForUserTagRobot;        //用户标签机器人使用的用户队列引用
        private UserQueue queueUserForUserInfoRobot;        //用户信息机器人使用的用户队列引用
        private UserQueue queueUserForUserRelationRobot;    //用户关系机器人使用的用户队列引用
        private UserQueue queueUserForStatusRobot;          //微博机器人使用的用户队列引用

        //构造函数，需要传入相应的新浪微博API和主界面
        public UserTagRobot(SinaApiService oAPI, UserQueue qUserForUserInfoRobot, UserQueue qUserForUserRelationRobot, UserQueue qUserForUserTagRobot, UserQueue qUserForStatusRobot)
            : base( oAPI )
        {
            strLogFile = Application.StartupPath + "\\" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString() + "_tag.log";
            queueUserForUserInfoRobot = qUserForUserInfoRobot;
            queueUserForUserRelationRobot = qUserForUserRelationRobot;
            queueUserForUserTagRobot = qUserForUserTagRobot;
            queueUserForStatusRobot = qUserForStatusRobot;
        }

        /// <summary>
        /// 以指定的UserID为起点开始爬行
        /// </summary>
        /// <param name="lUid"></param>
        public void Start ( long lStartUserID )
        {
            if (lStartUserID == 0) return;
            //将起始UserID入队
            queueUserForUserRelationRobot.Enqueue(lStartUserID);
            queueUserForUserInfoRobot.Enqueue(lStartUserID);
            queueUserForUserTagRobot.Enqueue(lStartUserID);
            queueUserForStatusRobot.Enqueue(lStartUserID);
            lCurrentID = lStartUserID;
            //对队列无限循环爬行，直至有操作暂停或停止
            while (true)
            {
                if (blnAsyncCancelled) return;
                while (blnSuspending)
                {
                    if (blnAsyncCancelled) return;
                    Thread.Sleep( 50 );
                }

                //将队头取出
                lCurrentID = queueUserForUserTagRobot.RollQueue();

                #region 预处理
                if (lCurrentID == lStartUserID)  //说明经过一次循环迭代
                {
                    if (blnAsyncCancelled) return;
                    while (blnSuspending)
                    {
                        if (blnAsyncCancelled) return;
                        Thread.Sleep( 50 );
                    }

                    //日志
                    Log( "开始爬行之前增加迭代次数..." );

                    Tag.NewIterate();
                    UserTag.NewIterate();
                }
                //日志
                Log( "记录当前用户ID：" + lCurrentID.ToString() );
                SysArg.SetCurrentUserIDForUserTag( lCurrentID );
                #endregion
                #region 用户标签信息
                if (blnAsyncCancelled) return;
                while (blnSuspending)
                {
                    if (blnAsyncCancelled) return;
                    Thread.Sleep( 50 );
                }

                //日志
                Log( "爬取用户" + lCurrentID.ToString() + "的标签..." );
                LinkedList<Tag> lstTag = crawler.GetTagsOf( lCurrentID );
                //日志
                Log( "爬得" + lstTag.Count.ToString() + "个标签。" );

                while (lstTag.Count > 0)
                {
                    if (blnAsyncCancelled) return;
                    while (blnSuspending)
                    {
                        if (blnAsyncCancelled) return;
                        Thread.Sleep( 50 );
                    }
                    Tag tag = lstTag.First.Value;
                    if (!Tag.Exists( tag.tag_id ))
                    {
                        //日志
                        Log( "将标签" + tag.tag_id.ToString() + "存入数据库..." );
                        tag.Add();
                    }
                    else
                        //日志
                        Log( "标签" + tag.tag_id.ToString() + "已存在。" );

                    if (!UserTag.Exists( lCurrentID, tag.tag_id ))
                    {
                        //日志
                        Log( "记录用户" + lCurrentID.ToString() + "拥有标签" + tag.tag_id.ToString() + "..." );
                        UserTag user_tag = new UserTag();
                        user_tag.user_id = lCurrentID;
                        user_tag.tag_id = tag.tag_id;
                        user_tag.Add();
                    }
                    else
                        //日志
                        Log( "用户" + lCurrentID.ToString() + "拥有标签" + tag.tag_id.ToString() + "已存在。" );

                    lstTag.RemoveFirst();
                }
                #endregion
                //日志
                Log( "用户" + lCurrentID.ToString() + "的标签数据已爬取完毕。" );
                //日志
                AdjustFreq();
                Log( "调整请求间隔为" + crawler.SleepTime.ToString() + "毫秒。本小时剩余" + crawler.ResetTimeInSeconds.ToString() + "秒，剩余请求次数为" + crawler.RemainingHits.ToString() + "次" );
            }
        }

        public override void Initialize ()
        {
            //初始化相应变量
            blnAsyncCancelled = false;
            blnSuspending = false;
            crawler.StopCrawling = false;
            queueUserForUserTagRobot.Initialize();
        }
    }
}
